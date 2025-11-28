using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApp.Api.Data;
using TodoApp.Api.Entities.Models;

namespace TodoApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TodosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TodosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _context.TodoItems.ToListAsync());

        [HttpPost]
        public async Task<IActionResult> Create(TodoItem item)
        {
            _context.TodoItems.Add(item);
            await _context.SaveChangesAsync();
            return Ok(item);
        }
    }

}
