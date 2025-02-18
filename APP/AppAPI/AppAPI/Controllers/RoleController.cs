﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AppAPI.Data;
using AppAPI.Models;
using AppAPI.Models.Domain;
using AppAPI.Models.DTO;
using TodoAPI.Models;

namespace TodoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RoleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Role
        [HttpGet("GetAllRoles")] //ok
        public async Task<ActionResult<IEnumerable<Role>>> GetRoles()
        {
            var roles = await _context.Roles.ToListAsync();
            return Ok(roles);
        }

        [HttpGet("GetUserRolesByID")] //ok
        public ActionResult<ApiResponse<List<string>>> GetUserRoles(Guid userId)
        {
            try
            {
                // Select role names for the user, joining UserRole and Role entities
                var roles = _context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .Select(ur => ur.Role!.RoleName) // Select RoleName instead of RoleId
                    .ToList();

                if (!roles.Any())
                {
                    return NotFound(new ApiResponse<List<string>>
                    {
                        Success = false,
                        Message = "User has no roles assigned",
                    });
                }

                return Ok(new ApiResponse<List<string>>
                {
                    Success = true,
                    Message = "Roles retrieved successfully",
                    Data = roles
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = ex.Message,
                });
            }
        }

        // POST: api/Role
        [HttpPost("AddRole")] //ok
        public async Task<ActionResult<ApiResponse<Role>>> AddRole(RoleDTO role)
        {
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role), "The role object cannot be null.");
            }

            if (string.IsNullOrEmpty(role.RoleName))
            {
                throw new ArgumentException("Role name is required.", nameof(role.RoleName));
            }

            // Convert role name to lowercase
            var roleNameLowerCase = role.RoleName.ToLower();

            // Check if the role name already exists (case-insensitive)
            if (await _context.Roles.AnyAsync(r => r.RoleName.ToLower() == roleNameLowerCase))
            {
                return BadRequest(new ApiResponse<Role>
                {
                    Message = "Role name already exists",
                    Success = false,
                });
            }

            // Create new role
            var newRole = new Role
            {
                RoleId = Guid.NewGuid(),
                RoleName = roleNameLowerCase, // Save the role name in lowercase
            };

            _context.Roles.Add(newRole);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<Role>
            {
                Message = "Role added successfully",
                Success = true,
                Data = newRole
            });
        }


        // DELETE: api/Role/{id}
        [HttpDelete("RemoveRole")] //ok
        public async Task<IActionResult> RemoveRole(Guid id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPut("UpdateRole")] //ok
        public async Task<IActionResult> UpdateRole(Guid Id, RoleDTO role)
        {
            if (Id == Guid.Empty)
            {
                return BadRequest("Invalid role ID.");
            }

            // Check if the role object is null or if the RoleName is empty or invalid
            if (role == null)
            {
                return BadRequest("Role data is required.");
            }

            if (string.IsNullOrEmpty(role.RoleName))
            {
                return BadRequest("Role name cannot be empty.");
            }

            // Convert role name to lowercase
            var roleNameLowerCase = role.RoleName.ToLower();

            // Check if the role name already exists (case-insensitive)
            var existingRoleWithName = await _context.Roles
                .Where(r => r.RoleName.ToLower() == roleNameLowerCase && r.RoleId != Id) // Exclude the current role being updated
                .FirstOrDefaultAsync();

            if (existingRoleWithName != null)
            {
                return BadRequest("Role name already exists.");
            }

            var existingRole = await _context.Roles.FindAsync(Id);
            if (existingRole == null)
            {
                return NotFound("Role not found.");
            }

            // Update the role name
            existingRole.RoleName = roleNameLowerCase; // Save the role name in lowercase

            try
            {
                // Save the changes asynchronously
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the exception (consider using a logger here)
                return StatusCode(500, $"An error occurred while updating the role: {ex.Message}");
            }

            return Ok(new { Message = "Role updated successfully.", Role = existingRole });
        }
    }
}
