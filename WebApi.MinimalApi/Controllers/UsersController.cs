using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;

    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(mapper.Map<UserDto>(user));
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] CreateUserDto createUserDto)
    {
        if (createUserDto is null)
        {
            return BadRequest();
        }

        if (TryFindLoginFormatError(createUserDto.Login))
        {
            return UnprocessableEntity(ModelState);
        }

        var createdUserEntity = userRepository.Insert(mapper.Map<UserEntity>(createUserDto));

        return ToCreatedAtRouteResult(createdUserEntity);
    }

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromRoute] string userId, [FromBody] UpdateUserDto updateUserDto)
    {
        if (updateUserDto is null || !Guid.TryParse(userId, out var id))
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var userToUpdate = mapper.Map<UserEntity>(updateUserDto);

        if (userRepository.FindById(id) == null)
        {
            var createdUserEntity = userRepository.Insert(userToUpdate);
            return ToCreatedAtRouteResult(createdUserEntity);
        }

        userRepository.Update(userToUpdate);
        return NoContent();
    }

    private bool TryFindLoginFormatError(string login)
    {
        if (string.IsNullOrEmpty(login) || !login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("login", "Error");
            return true;
        }

        return false;
    }

    public CreatedAtRouteResult ToCreatedAtRouteResult(UserEntity userEntity)
    {
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = userEntity.Id },
            userEntity.Id);
    }
}