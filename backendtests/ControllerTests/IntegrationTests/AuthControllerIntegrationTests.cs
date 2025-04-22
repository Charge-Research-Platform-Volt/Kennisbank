using KnowledgeBank.Models;
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
using System.Security.Claims;

namespace backend.Tests.Integration;

[TestFixture]
[Category("IntegrationTest")]
public class AuthControllerTests : TestBase
{
    private AuthController _controller;
    private UserManager<User> _userManager;
    private SignInManager<User> _signInManager;
    private Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private Mock<IUserClaimsPrincipalFactory<User>> _mockUserClaimsPrincipalFactory;
    private Mock<HttpContext> _mockHttpContext;
    private UserStore<User> _userStore;

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
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockHttpContext = new Mock<HttpContext>();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockUserClaimsPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
        _userStore = new UserStore<User>(Context);

        _userManager = new UserManager<User>(
           _userStore,
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
            _mockHttpContextAccessor.Object,
            _mockUserClaimsPrincipalFactory.Object,
            null, null, null, null
        );

        _controller = new AuthController(_signInManager, Context);
    }

    protected override Task OnTestTearDown()
    {
        _userManager?.Dispose();
        _userStore?.Dispose();
        return base.OnTestTearDown();
    }

    [Test]
    public async Task SendInvitation_CreatesInvitation_WithCorrectParameters()
    {
        // Arrange
        string email = "test@test.nl";
        string hashedEmail = ShaUtils.Sha256(email);
        DateTime startTime = DateTime.UtcNow;

        // Act
        IActionResult result = await _controller.Invite(email);

        // Assert
        Assert.That(result, Is.TypeOf<OkResult>(), "The result must be an OkResult.");
        
        Assert.That(Context.Invitations.Count(), Is.EqualTo(1), "The number of invitations in the database must be 1.");
        Invitation? invitation = Context.Invitations.FirstOrDefault(i => i.Email == hashedEmail);
        Assert.That(invitation, Is.Not.Null, "There must be an invitation with the hashed email in the database.");
        Assert.That(invitation.Email, Is.EqualTo(hashedEmail), "The email must be the hashed email of the user.");
        Assert.That(invitation.CreatedAt, Is.LessThanOrEqualTo(DateTime.UtcNow), "The creation date of the invitation must be set correctly.");
        Assert.That(invitation.CreatedAt, Is.GreaterThanOrEqualTo(startTime), "The creation date of the invitation must be set correctly.");
    }

    [Test]
    public async Task SignUp_ReturnsBadRequest_WithWrongEmail()
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
        IActionResult result = await _controller.Register(new SignUpDto(){
            Email = "wrong@email.com",
            Password = "Test123!",
            Token = token,
        });

        // Assert
        BadRequestObjectResult? badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null, "The result must be a BadRequestObjectResult.");
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(400), "The statuscode must be 400.");

        IdentityUser? user = Context.Users.FirstOrDefault(u => u.Email == email);
        Assert.That(user, Is.Null, "The user must not be created in the database.");
    }

    [Test]
    public async Task SignUp_ReturnsBadRequest_WithWrongToken()
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
        IActionResult result = await _controller.Register(new SignUpDto(){
            Email = email,
            Password = "Test123!",
            Token = "wrongtoken",
        });

        // Assert
        BadRequestObjectResult? badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null, "The result must be a BadRequestObjectResult.");
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(400), "The statuscode must be 400.");

        IdentityUser? user = Context.Users.FirstOrDefault(u => u.Email == email);
        Assert.That(user, Is.Null, "The user must not be created in the database.");
    }

    [Test]
    public async Task SignUp_ReturnsBadRequest_WithMixedUpTokenEmail()
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
        IActionResult result = await _controller.Register(new SignUpDto(){
            Email = email1,
            Password = "Test123!",
            Token = token2,
        });

        // Assert
        BadRequestObjectResult? badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null, "The result must be a BadRequestObjectResult.");
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(400), "The statuscode must be 400.");

        IdentityUser? user1 = Context.Users.FirstOrDefault(u => u.Email == email1);
        Assert.That(user1, Is.Null, "The user must not be created in the database.");
        IdentityUser? user2 = Context.Users.FirstOrDefault(u => u.Email == email2);
        Assert.That(user2, Is.Null, "The user must not be created in the database.");
    }

    [Test]
    public async Task SignUp_ReturnsBadRequest_WithOutdatedInvite()
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
        IActionResult result = await _controller.Register(new SignUpDto(){
            Email = email,
            Password = "Test123!",
            Token = token,
        });

        // Assert
        BadRequestObjectResult? badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null, "The result must be a BadRequestObjectResult.");
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(400), "The statuscode must be 400.");

        IdentityUser? user = Context.Users.FirstOrDefault(u => u.Email == email);
        Assert.That(user, Is.Null, "The user must not be created in the database.");
    }

    [Test]
    public async Task SignUp_CreatesUser_WithCorrectData()
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
        IActionResult result = await _controller.Register(new SignUpDto(){
            Email = email,
            Password = "Test123!",
            Token = token,
        });

        // Assert
        Assert.That(result, Is.TypeOf<OkObjectResult>(), "The result must be an OkResult.");

        IdentityUser? user = Context.Users.FirstOrDefault(u => u.Email == email);
        Assert.That(user, Is.Not.Null, "The user must be created in the database.");
    }

    [Test]
    public async Task SignUp_ReturnsBadRequest_WithoutInvite()
    {
        // Arrange
        string email = "test@test.nl";
        string password = "Test123!";

        // Act
        IActionResult result = await _controller.Register(new SignUpDto(){
            Email = email,
            Password = password,
            Token = ""
        });

        // Assert
        BadRequestObjectResult? badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult?.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task SignIn_Succesful_WithCorrectCredentials()
    {
        // Arrange
        string email = "test@test.nl";
        string password = "Test123!";

        // Create user
        User user = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            NormalizedEmail = email.ToUpper(),
            NormalizedUserName = email.ToUpper(),
            EmailConfirmed = true
        };
        
        await _userManager.CreateAsync(user, password);
        await _userManager.AddToRoleAsync(user, "user");

        Microsoft.AspNetCore.Identity.SignInResult mockSignInResult = Microsoft.AspNetCore.Identity.SignInResult.Success;
        Mock<SignInManager<User>> mockSignInManager = new Mock<SignInManager<User>>(
            _userManager,
            _mockHttpContextAccessor.Object,
            new Mock<IUserClaimsPrincipalFactory<User>>().Object,
            null, null, null, null
        );
        
        mockSignInManager
            .Setup(sm => sm.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(mockSignInResult);


        // Assert
        Microsoft.AspNetCore.Identity.SignInResult result = await mockSignInManager.Object.PasswordSignInAsync(email, password, false, false);
        Assert.That(result.Succeeded, Is.True, "The sign-in should succeed with correct credentials");
    }

    [Test]
    public async Task SignIn_Unsuccesful_WithIncorrectCredentials()
    {
        // Arrange
        string email = "test@test.nl";
        string password = "Test123!";
        string wrongPassword = "WrongPassword123!";

        // Create user
        User user = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            NormalizedEmail = email.ToUpper(),
            NormalizedUserName = email.ToUpper(),
            EmailConfirmed = true
        };
        
        await _userManager.CreateAsync(user, password);
        await _userManager.AddToRoleAsync(user, "user");

        Microsoft.AspNetCore.Identity.SignInResult mockSignInResult = Microsoft.AspNetCore.Identity.SignInResult.Failed;
        Mock<SignInManager<User>> mockSignInManager = new Mock<SignInManager<User>>(
            _userManager,
            _mockHttpContextAccessor.Object,
            new Mock<IUserClaimsPrincipalFactory<User>>().Object,
            null, null, null, null
        );
        
        mockSignInManager
            .Setup(sm => sm.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(mockSignInResult);

        // Assert
        Microsoft.AspNetCore.Identity.SignInResult result = await mockSignInManager.Object.PasswordSignInAsync(email, wrongPassword, false, false);
        Assert.That(result.Succeeded, Is.False, "The sign-in should fail with incorrect credentials");
    }

    [Test]
    public void Ping_ReturnsEmail_WhenAuthenticated()
        {
        // Arrange
        string email = "test@test.nl";
        object expectedReturnValue = new { Email = email };

        // Create an authenticated user with the email claim
        List<Claim> claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, email)
        };

        ClaimsIdentity identity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);

        // Set up the mock HttpContext with the authenticated user
        Mock<HttpContext> mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(principal);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = mockHttpContext.Object
        };

        // Act
        IActionResult result = _controller.Ping();
        
        // Assert
        Assert.That(result, Is.TypeOf<OkObjectResult>(), "The result must be an OkObjectResult.");
        OkObjectResult? okResult = result as OkObjectResult;
        Assert.That(okResult?.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
        
        // Verify the email is returned in the response
        Assert.That(ObjectComparer.AreObjectsEqual(okResult.Value, expectedReturnValue), 
            "The response should contain the user's email.");
    }

    [Test]
    public async Task Logout_ReturnsOkResult_WithValidRequest()
    {
        // Arrange
        Mock<SignInManager<User>> mockSignInManager = new Mock<SignInManager<User>>(
            _userManager,
            _mockHttpContextAccessor.Object,
            new Mock<IUserClaimsPrincipalFactory<User>>().Object,
            null, null, null, null
        );
        
        mockSignInManager
            .Setup(sm => sm.SignOutAsync())
            .Returns(Task.CompletedTask);
        
        _controller = new AuthController(mockSignInManager.Object, Context);

        // Simulate an authenticated user
        List<Claim> claims = new List<Claim> { new Claim(ClaimTypes.Name, "test@test.nl") };
        ClaimsIdentity identity = new ClaimsIdentity(claims, "TestAuth");
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);

        Mock<HttpContext> mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(principal);
        mockHttpContext.Setup(c => c.User.Identity.IsAuthenticated).Returns(true);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = mockHttpContext.Object
        };

        // Act
        IActionResult result = await _controller.Logout(new {});
        
        // Assert
        Assert.That(result, Is.TypeOf<OkResult>(), "The result must be an OkResult.");
        OkResult? okResult = result as OkResult;
        Assert.That(okResult?.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
        
        // Verify the SignOutAsync method was called once
        mockSignInManager.Verify(sm => sm.SignOutAsync(), Times.Once);
    }

    [Test]
    public async Task Logout_ReturnsUnauthorized_WithNullRequest()
    {
        // Arrange
        Mock<SignInManager<User>> mockSignInManager = new Mock<SignInManager<User>>(
            _userManager,
            _mockHttpContextAccessor.Object,
            new Mock<IUserClaimsPrincipalFactory<User>>().Object,
            null, null, null, null
        );
        
        _controller = new AuthController(mockSignInManager.Object, Context);

        // Simulate an authenticated user
        List<Claim> claims = new List<Claim> { new Claim(ClaimTypes.Name, "test@test.nl") };
        ClaimsIdentity identity = new ClaimsIdentity(claims, "TestAuth");
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);

        Mock<HttpContext> mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(principal);
        mockHttpContext.Setup(c => c.User.Identity.IsAuthenticated).Returns(true);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = mockHttpContext.Object
        };

        // Act
        IActionResult result = await _controller.Logout(null);
        
        // Assert
        Assert.That(result, Is.TypeOf<UnauthorizedResult>(), "The result must be an UnauthorizedResult.");
        UnauthorizedResult? unauthorizedResult = result as UnauthorizedResult;
        Assert.That(unauthorizedResult?.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
        
        // Verify the SignOutAsync method was NOT called
        mockSignInManager.Verify(sm => sm.SignOutAsync(), Times.Never);
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


