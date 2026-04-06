using Microsoft.AspNetCore.Identity;

namespace VAH.Backend.Tests.Builders;

/// <summary>
/// Builder for creating test IdentityUser entities using the builder pattern.
/// </summary>
public class UserBuilder
{
    private readonly IdentityUser _user;

    public UserBuilder()
    {
        _user = new IdentityUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            Email = "testuser@example.com",
            EmailConfirmed = true,
            NormalizedEmail = "TESTUSER@EXAMPLE.COM",
            NormalizedUserName = "TESTUSER",
            SecurityStamp = Guid.NewGuid().ToString()
        };
    }

    public UserBuilder WithId(string id)
    {
        _user.Id = id;
        return this;
    }

    public UserBuilder WithUserName(string userName)
    {
        _user.UserName = userName;
        _user.NormalizedUserName = userName.ToUpper();
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _user.Email = email;
        _user.NormalizedEmail = email.ToUpper();
        return this;
    }

    public UserBuilder WithEmailConfirmed(bool confirmed)
    {
        _user.EmailConfirmed = confirmed;
        return this;
    }

    public UserBuilder WithPhoneNumber(string phoneNumber)
    {
        _user.PhoneNumber = phoneNumber;
        return this;
    }

    public UserBuilder WithPhoneNumberConfirmed(bool confirmed)
    {
        _user.PhoneNumberConfirmed = confirmed;
        return this;
    }

    public UserBuilder WithTwoFactorEnabled(bool enabled)
    {
        _user.TwoFactorEnabled = enabled;
        return this;
    }

    public UserBuilder WithLockoutEnd(DateTimeOffset? lockoutEnd)
    {
        _user.LockoutEnd = lockoutEnd;
        return this;
    }

    public UserBuilder WithLockoutEnabled(bool enabled)
    {
        _user.LockoutEnabled = enabled;
        return this;
    }

    /// <summary>
    /// Builds and returns the IdentityUser entity.
    /// </summary>
    public IdentityUser Build()
    {
        return _user;
    }
}