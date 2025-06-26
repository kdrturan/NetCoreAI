using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCoreAI.Project01_ApiDemo.Context;
using NetCoreAI.Project01_ApiDemo.Entities;

namespace NetCoreAI.Project01_ApiDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly ApiContext _context;
        public CustomersController(ApiContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult CustomerList()
        {
            var customers = _context.Customers.ToList();
            return Ok(customers);
        }

        [HttpPost]
        public IActionResult CreateCustomer(Customer customer)
        {
            if (customer == null)
            {
                return BadRequest("Customer data is null");
            }
            _context.Customers.Add(customer);
            _context.SaveChanges();
            return Ok("Customer created successfully");
        }


        [HttpDelete]
        public IActionResult DeleteCustomer(int id)
        {
            var existingCustomer = _context.Customers.Find(id);
            if (existingCustomer == null)
            {
                return NotFound("Customer not found");
            }
            _context.Customers.Remove(existingCustomer);
            _context.SaveChanges();
            return Ok("Customer deleted successfully");
        }

        [HttpGet("id")]
        public IActionResult GetCustomerById(int id)
        {
            var customer = _context.Customers.Find(id);
            if (customer == null)
            {
                return NotFound("Customer not found");
            }
            return Ok(customer);
        }

        [HttpPut]
        public IActionResult UpdateCustomer(Customer customer)
        {
            if (customer == null)
            {
                return BadRequest("Customer data is null");
            }
            var existingCustomer = _context.Customers.Find(customer.CustomerId);
            if (existingCustomer == null)
            {
                return NotFound("Customer not found");
            }
            existingCustomer.CustomerName = customer.CustomerName;
            existingCustomer.CustomerLastName = customer.CustomerLastName;
            existingCustomer.CustomerBalance = customer.CustomerBalance;
            _context.SaveChanges();
            return Ok("Customer updated successfully");
        }

    }
}
