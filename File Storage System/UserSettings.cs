// UserSettings.cs
// This class defines the data structure for a user account.

namespace FileFlow
{
    public class UserSettings
    {
        public string? Username { get; set; }
        public string? Password { get; set; } // Note: In a real app, this should be securely hashed and salted.
    }
}
