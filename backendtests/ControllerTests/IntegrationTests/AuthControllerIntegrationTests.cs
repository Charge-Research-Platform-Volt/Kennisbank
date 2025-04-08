using backend.Controllers;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using backend.Tests.Infrastructure;
using KnowledgeBank.Controllers;
using KnowledgeBank.Models;
using KnowledgeBank.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Moq;
using Microsoft.AspNetCore.Http;
using KnowledgeBank.Utils;
using System.Net;

namespace backend.Tests.Integration;

[TestFixture]
[Category("IntegrationTest")]
public class AuthControllerTests : TestBase
{
    private AuthController _controller;
    private UserManager<User> _userManager;
    private SignInManager<User> _signInManager;

    protected override Task SeedTemplateDatabase(DatabaseContext context)
    {
        // Add role that all tests will need
        if (!context.Roles.Any(r => r.Name == "user"))
        {
            context.Roles.Add(new IdentityRole
            {
                Name = "user",
                NormalizedName = "USER"
            });
            context.SaveChanges();
        }
        
        return base.SeedTemplateDatabase(context);
    }

    [SetUp]
    public void SetupController()
    {
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockUserClaimsPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
        var userStore = new UserStore<User>(Context);

        _userManager = new UserManager<User>(
           userStore,
            null,
            new PasswordHasher<User>(),
            new[] { new UserValidator<User>() },
            new[] { new PasswordValidator<User>() }, 
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null, 
            null 
        );
        _signInManager = new SignInManager<User>(
            _userManager,
            mockHttpContextAccessor.Object,
            mockUserClaimsPrincipalFactory.Object,
            null, null, null, null
        );

        _controller = new AuthController(_signInManager, Context);
    }

    protected override Task OnTestTearDown()
    {
        _userManager?.Dispose();
        return base.OnTestTearDown();
    }

    [Test]
    public async Task SendInvitation()
    {
        // Arrange
        string email = "test@test.nl";
        string hashedEmail = ShaUtils.Sha256(email);
        DateTime startTime = DateTime.UtcNow;

        // Act
        var result = await _controller.Invite(email);

        // Assert
        Assert.That(result, Is.TypeOf<OkResult>(), "The result must be an OkResult.");
        
        Assert.That(Context.Invitations.Count(), Is.EqualTo(1), "The number of invitations in the database must be 1.");
        var invitation = Context.Invitations.FirstOrDefault(i => i.Email == hashedEmail);
        Assert.That(invitation, Is.Not.Null, "There must be an invitation with the hashed email in the database.");
        Assert.That(invitation.Email, Is.EqualTo(hashedEmail), "The email must be the hashed email of the user.");
        Assert.That(invitation.CreatedAt, Is.LessThanOrEqualTo(DateTime.UtcNow), "The creation date of the invitation must be set correctly.");
        Assert.That(invitation.CreatedAt, Is.GreaterThanOrEqualTo(startTime), "The creation date of the invitation must be set correctly.");
    }

    [Test]
    public async Task SignUpWithWrongEmail()
    {
        // Arrange
        string email = "test@test.nl";
        string hashedEmail = ShaUtils.Sha256(email);
        string token = Guid.NewGuid().ToString();
        string hashedToken = ShaUtils.Sha256(token);

        await Context.Invitations.AddAsync(new Invitation()
        {
            Id = Guid.NewGuid(),
            Email = hashedEmail,
            Token = hashedToken,
            CreatedAt = DateTime.UtcNow
        });
        await Context.SaveChangesAsync();

        // Act
        var result = await _controller.Register(new SignUpDto(){
            Email = "wrong@email.com",
            Password = "Test123!",
            Token = token,
        });

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null, "The result must be a BadRequestObjectResult.");
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(400), "The statuscode must be 400.");

        var user = Context.Users.FirstOrDefault(u => u.Email == email);
        Assert.That(user, Is.Null, "The user must not be created in the database.");
    }

    [Test]
    public async Task SignUpWithWrongToken()
    {
        // Arrange
        string email = "test@test.nl";
        string hashedEmail = ShaUtils.Sha256(email);
        string token = Guid.NewGuid().ToString();
        string hashedToken = ShaUtils.Sha256(token);

        await Context.Invitations.AddAsync(new Invitation()
        {
            Id = Guid.NewGuid(),
            Email = hashedEmail,
            Token = hashedToken,
            CreatedAt = DateTime.UtcNow
        });
        await Context.SaveChangesAsync();

        // Act
        var result = await _controller.Register(new SignUpDto(){
            Email = email,
            Password = "Test123!",
            Token = "wrongtoken",
        });

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null, "The result must be a BadRequestObjectResult.");
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(400), "The statuscode must be 400.");

        var user = Context.Users.FirstOrDefault(u => u.Email == email);
        Assert.That(user, Is.Null, "The user must not be created in the database.");
    }

    [Test]
    public async Task SignUpWithMixedUpTokenEmail()
    {
        // Arrange
        string email1 = "test@test.nl";
        string hashedEmail1 = ShaUtils.Sha256(email1);
        string email2 = "test@test.nl";
        string hashedEmail2 = ShaUtils.Sha256(email2);
        string token1 = Guid.NewGuid().ToString();
        string hashedToken1 = ShaUtils.Sha256(token1);
        string token2 = Guid.NewGuid().ToString();
        string hashedToken2 = ShaUtils.Sha256(token1);

        await Context.Invitations.AddAsync(new Invitation()
        {
            Id = Guid.NewGuid(),
            Email = hashedEmail1,
            Token = hashedToken1,
            CreatedAt = DateTime.UtcNow
        });

        await Context.Invitations.AddAsync(new Invitation()
        {
            Id = Guid.NewGuid(),
            Email = hashedEmail2,
            Token = hashedToken2,
            CreatedAt = DateTime.UtcNow
        });
        await Context.SaveChangesAsync();

        // Act
        var result = await _controller.Register(new SignUpDto(){
            Email = email1,
            Password = "Test123!",
            Token = token2,
        });

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null, "The result must be a BadRequestObjectResult.");
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(400), "The statuscode must be 400.");

        var user1 = Context.Users.FirstOrDefault(u => u.Email == email1);
        Assert.That(user1, Is.Null, "The user must not be created in the database.");
        var user2 = Context.Users.FirstOrDefault(u => u.Email == email2);
        Assert.That(user2, Is.Null, "The user must not be created in the database.");
    }

    [Test]
    public async Task SignWithOutdatedInvite()
    {
        // Arrange
        string email = "test@test.nl";
        string hashedEmail = ShaUtils.Sha256(email);
        string token = Guid.NewGuid().ToString();
        string hashedToken = ShaUtils.Sha256(token);

        await Context.Invitations.AddAsync(new Invitation()
        {
            Id = Guid.NewGuid(),
            Email = hashedEmail,
            Token = hashedToken,
            CreatedAt = DateTime.UtcNow.AddDays(-8),
        });
        await Context.SaveChangesAsync();

        // Act
        var result = await _controller.Register(new SignUpDto(){
            Email = email,
            Password = "Test123!",
            Token = token,
        });

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null, "The result must be a BadRequestObjectResult.");
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(400), "The statuscode must be 400.");

        var user = Context.Users.FirstOrDefault(u => u.Email == email);
        Assert.That(user, Is.Null, "The user must not be created in the database.");
    }

    [Test]
    public async Task SignUpWithCorrectData()
    {
        // Arrange
        string email = "test@test.nl";
        string hashedEmail = ShaUtils.Sha256(email);
        string token = Guid.NewGuid().ToString();
        string hashedToken = ShaUtils.Sha256(token);

        await Context.Invitations.AddAsync(new Invitation()
        {
            Id = Guid.NewGuid(),
            Email = hashedEmail,
            Token = hashedToken,
            CreatedAt = DateTime.UtcNow
        });
        await Context.SaveChangesAsync();

        // Act
        var result = await _controller.Register(new SignUpDto(){
            Email = email,
            Password = "Test123!",
            Token = token,
        });

        // Assert
        Assert.That(result, Is.TypeOf<OkObjectResult>(), "The result must be an OkResult.");

        var user = Context.Users.FirstOrDefault(u => u.Email == email);
        Assert.That(user, Is.Not.Null, "The user must be created in the database.");
    }

    [Test]
    public async Task SignUpWithoutInvite()
    {
        // Arrange
        string email = "test@test.nl";
        string password = "Test123!";

        // Act
        var result = await _controller.Register(new SignUpDto(){
            Email = email,
            Password = password,
            Token = ""
        });

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult?.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
    }
}