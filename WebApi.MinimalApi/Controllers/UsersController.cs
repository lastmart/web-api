using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;

    private readonly IMapper mapper;

    private readonly LinkGenerator linkGenerator;


    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация

    public UsersController(
        IUserRepository userRepository,
        IMapper mapper,
        LinkGenerator linkGenerator
    )
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
        this.linkGenerator = linkGenerator;
    }

    [HttpOptions]
    public IActionResult Options()
    {
        Response.Headers.Add("Allow", "GET, POST, OPTIONS");
        return Ok();
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);

        if (user == null)
        {
            return NotFound();
        }

        if (Request.Method == HttpMethod.Head.Method)
        {
            Response.Body = Stream.Null;
        }

        return Ok(mapper.Map<UserDto>(user));
    }

    [HttpGet]
    [Produces("application/json", "application/xml")]
    public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1)
            pageNumber = 1;

        if (pageSize < 1)
            pageSize = 1;

        if (pageSize > 20)
            pageSize = 20;

        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);
        AddPaginationInfoInHeaders(pageNumber, pageSize, pageList);
        return Ok(users);
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

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser(string userId)
    {
        if (!Guid.TryParse(userId, out var id))
        {
            return NotFound();
        }

        if (userRepository.FindById(id) == null)
        {
            return NotFound();
        }

        userRepository.Delete(id);
        return NoContent();
    }

    private void AddPaginationInfoInHeaders(int pageNumber, int pageSize, PageList<UserEntity> pageList)
    {
        var paginationHeader = new
        {
            previousPageLink = pageList.HasPrevious
                ? linkGenerator.GetUriByAction(
                    HttpContext, nameof(GetUsers),
                    values: new { pageNumber = pageNumber - 1, pageSize })
                : null,
            nextPageLink = pageList.HasNext
                ? linkGenerator.GetUriByAction(
                    HttpContext, nameof(GetUsers),
                    values: new { pageNumber = pageNumber + 1, pageSize })
                : null,
            totalCount = pageList.TotalCount,
            pageSize = pageSize,
            currentPage = pageNumber,
            totalPages = (int)Math.Ceiling((double)pageList.TotalCount / pageSize)
        };

        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
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