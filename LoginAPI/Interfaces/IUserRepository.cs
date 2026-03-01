using LoginAPI.Data.Entities;

namespace LoginAPI.Interfaces;

/// <summary>
/// Provides persistence operations for <see cref="User"/> entities.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Retrieves a user by email address.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <returns>The matching user when found; otherwise <see langword="null"/>.</returns>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Retrieves a user by unique identifier.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <returns>The matching user when found; otherwise <see langword="null"/>.</returns>
    Task<User?> GetByIdAsync(int id);

    /// <summary>
    /// Retrieves all users.
    /// </summary>
    /// <returns>A list of users.</returns>
    Task<List<User>> GetAllAsync();

    /// <summary>
    /// Persists a new user.
    /// </summary>
    /// <param name="user">The user entity to create.</param>
    /// <returns>The created user entity.</returns>
    Task<User> CreateAsync(User user);

    /// <summary>
    /// Determines whether a user already exists for a given email.
    /// </summary>
    /// <param name="email">The email address to check.</param>
    /// <returns><see langword="true"/> if a user exists; otherwise <see langword="false"/>.</returns>
    Task<bool> EmailExistsAsync(string email);
}
