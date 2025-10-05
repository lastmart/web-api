using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
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

        if (TryFindLoginFormatError(createUserDto.Login, out var error))
        {
            ModelState.AddModelError("login", error);
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

    [HttpPatch("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult PatchUser([FromRoute] string userId, [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
    {
        if (patchDoc == null)
        {
            return BadRequest();
        }

        if (!Guid.TryParse(userId, out var id))
        {
            return NotFound();
        }

        var user = userRepository.FindById(id);
        if (user is null)
        {
            return NotFound();
        }

        var updateUserDto = mapper.Map<UpdateUserDto>(user);
        patchDoc.ApplyTo(updateUserDto, ModelState);

        if (TryValidateModel(updateUserDto))
        {
            userRepository.Update(mapper.Map<UserEntity>(updateUserDto));
        }

        return ModelState.IsValid
            ? NoContent()
            : UnprocessableEntity(ModelState);
    }

    private bool TryFindLoginFormatError(string login, [MaybeNullWhen(false)] out string error)
    {
        if (string.IsNullOrEmpty(login) || !login.All(char.IsLetterOrDigit))
        {
            error = "Login should contain only letters or digits";
            return true;
        }

        error = null;
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