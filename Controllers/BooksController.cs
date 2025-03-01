﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YJKBooks.Contexts;
using YJKBooks.Entities;
using YJKBooks.Models;
using YJKBooks.Extensions;
using Microsoft.AspNetCore.Http;
using YJKBooks.RequestHelper;

namespace YJKBooks.Controllers
{
    public class BooksController : BaseApiController
    {
        private readonly ApplicationDbContext _context;

        public BooksController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<PageList<BookDto>>> GetBooks([FromQuery]BookPrams bookPrams)
        {
            try
            {
                var query = _context.Books
                                 .Sort(bookPrams.OrderBy)
                                 .Search(bookPrams.SearchTerm)
                                 .Filter(bookPrams.Genres)
                                 .AsQueryable();
                
                //var books= await PageList<Book>.ToPagedlist(query, bookPrams.PageNumber,bookPrams.PageSize);
                
                var userId = Request.Cookies["userId"];

                var favouriteBookList = await _context.FavouriteBookList.Include(i => i.Items)
                                                                        .FirstOrDefaultAsync(x => x.UserId == userId);
                var favouriteBookIds = favouriteBookList?.Items.Select(x => x.BookId);
                if (favouriteBookIds != null) {
                    
                    var bookDtosQuery = (from book in query
                                         select new BookDto
                                         {
                                             Id = book.Id,
                                             Title = book.Title,
                                             Author = book.Author,
                                             Link = book.Link,
                                             Synopsis = book.Synopsis,
                                             Price = book.Price,
                                             PictureUrl = book.PictureUrl,
                                             IsFavourite = favouriteBookIds.Contains(book.Id),
                                             Genre = book.BookGenre
                                         });
                    var bookDtos = await PageList<BookDto>.ToPagedlist(bookDtosQuery, bookPrams.PageNumber, bookPrams.PageSize);
                    Response.AddPaginationHeader(bookDtos.MetaData);
                    return bookDtos;
                }
                else
                {
                    var bookDtosQuery = (from book in query
                                         select new BookDto
                                         {
                                             Id = book.Id,
                                             Title = book.Title,
                                             Author = book.Author,
                                             Link = book.Link,
                                             Synopsis = book.Synopsis,
                                             Price = book.Price,
                                             PictureUrl = book.PictureUrl,
                                             IsFavourite = false,
                                             Genre = book.BookGenre
                                         });
                    var bookDtos = await PageList<BookDto>.ToPagedlist(bookDtosQuery, bookPrams.PageNumber, bookPrams.PageSize);
                    Response.AddPaginationHeader(bookDtos.MetaData);
                    return bookDtos;

                }                                               
                


            }
            catch (Exception)
            {
                // add logging here?
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }            
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BookDto>> GetBook(int id)
        {
            var book = await _context.Books
                                     .Include(x => x.BookReviews)
                                     .ThenInclude(u => u.User)
                                     .Where(x => x.Id == id)
                                     .FirstOrDefaultAsync();
                                            
            if (book == null) return NotFound();

            var bookDto = new BookDto 
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                Link = book.Link,
                Synopsis = book.Synopsis,
                Price = book.Price,
                PictureUrl = book.PictureUrl,
                Genre = book.BookGenre,
                BookReviews = MapReviewDtos(book.BookReviews)
            };

            return Ok(bookDto);
        }
        
        private ICollection<ReviewDto> MapReviewDtos(ICollection<Reviews> reviews)
        {
            ReviewDto MapReviewToDto(Reviews review)
            {
                return new ReviewDto
                {
                    Id = review.Id,
                    PublishedDate = review.PublishedDate,
                    Title = review.Title,
                    UserId = review.UserId,
                    Description = review.Description,
                    BookId = review.BookId,
                    Rating = review.Rating,                
                    BookTitle = review.Book.Title,
                    UserName = review.User.UserName
                };
            }

            var reviewsDtos = new List<ReviewDto>();

            foreach(var review in reviews) {
                reviewsDtos.Add(MapReviewToDto(review));
            }

            return reviewsDtos;
        }       

        [HttpGet("filters")]
        public async Task<IActionResult> GetFilters()
        {
            //want unique genre (distinct) from the list of book
            var genres = await _context.Books.Select(p=>p.BookGenre).Distinct().ToListAsync();
            
            return Ok(new {genres});
        }

    }
}
