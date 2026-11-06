namespace CITWEB.Services;

public sealed class UserAccountService
{
    private readonly object _lock = new();
    private readonly Dictionary<string, UserRecord> _users = new(StringComparer.OrdinalIgnoreCase);

    public UserAccountService()
    {
        var admin = new UserRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            Email = "admin@gmail.com",
            FullName = "Hảo Admin",
            Password = "123",
            Role = AuthSessionExtensions.RoleAdmin,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            LastLoginAt = DateTimeOffset.UtcNow,
            Phone = "0900000000",
            Age = 28,
            Bio = "Quản trị viên hệ thống Serene.",
        };
        _users[admin.Email] = admin;
    }

    public bool TryRegister(string email, string password, string fullName, out string error, out UserRecord? createdUser)
    {
        email = (email ?? "").Trim();
        fullName = (fullName ?? "").Trim();
        password = password ?? "";

        if (string.IsNullOrWhiteSpace(fullName))
        {
            error = "Vui lòng nhập họ tên.";
            createdUser = null;
            return false;
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            error = "Email không hợp lệ.";
            createdUser = null;
            return false;
        }

        if (password.Length < 6)
        {
            error = "Mật khẩu phải có ít nhất 6 ký tự.";
            createdUser = null;
            return false;
        }

        lock (_lock)
        {
            if (_users.ContainsKey(email))
            {
                error = "Email này đã được đăng ký.";
                createdUser = null;
                return false;
            }

            var user = new UserRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                Email = email,
                FullName = fullName,
                Password = password,
                Role = AuthSessionExtensions.RoleUser,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            _users[email] = user;
            createdUser = Clone(user);
        }

        error = "";
        return true;
    }

    public bool TryLogin(string email, string password, out UserRecord? user)
    {
        email = (email ?? "").Trim();
        password = password ?? "";

        lock (_lock)
        {
            if (_users.TryGetValue(email, out var record) && record.Password == password)
            {
                if (!record.IsActive)
                {
                    user = null;
                    return false;
                }

                record.LastLoginAt = DateTimeOffset.UtcNow;
                user = Clone(record);
                return true;
            }
        }

        user = null;
        return false;
    }

    public bool TryGetByEmail(string email, out UserRecord? user)
    {
        email = (email ?? "").Trim();
        lock (_lock)
        {
            if (_users.TryGetValue(email, out var found))
            {
                user = Clone(found);
                return true;
            }
        }

        user = null;
        return false;
    }

    public IReadOnlyList<UserRecord> GetAllUsers()
    {
        lock (_lock)
        {
            return _users.Values
                .OrderByDescending(x => x.CreatedAt)
                .Select(Clone)
                .ToList();
        }
    }

    public bool AdminCreateUser(
        string fullName,
        string email,
        string password,
        string role,
        bool isActive,
        out string error)
    {
        fullName = (fullName ?? "").Trim();
        email = (email ?? "").Trim();
        password = password ?? "";
        role = NormalizeRole(role);

        if (string.IsNullOrWhiteSpace(fullName))
        {
            error = "Họ tên không được để trống.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            error = "Email không hợp lệ.";
            return false;
        }

        if (password.Length < 6)
        {
            error = "Mật khẩu phải có ít nhất 6 ký tự.";
            return false;
        }

        lock (_lock)
        {
            if (_users.ContainsKey(email))
            {
                error = "Email đã tồn tại.";
                return false;
            }

            _users[email] = new UserRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                FullName = fullName,
                Email = email,
                Password = password,
                Role = role,
                IsActive = isActive,
                CreatedAt = DateTimeOffset.UtcNow,
            };
        }

        error = "";
        return true;
    }

    public bool AdminUpdateUser(
        string id,
        string fullName,
        string role,
        bool isActive,
        string? newPassword,
        out string error)
    {
        fullName = (fullName ?? "").Trim();
        role = NormalizeRole(role);
        newPassword ??= "";

        if (string.IsNullOrWhiteSpace(id))
        {
            error = "Thiếu định danh người dùng.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            error = "Họ tên không được để trống.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(newPassword) && newPassword.Length < 6)
        {
            error = "Mật khẩu mới phải có ít nhất 6 ký tự.";
            return false;
        }

        lock (_lock)
        {
            var target = _users.Values.FirstOrDefault(x => x.Id == id);
            if (target is null)
            {
                error = "Không tìm thấy người dùng.";
                return false;
            }

            // Always keep at least one active admin.
            if (string.Equals(target.Role, AuthSessionExtensions.RoleAdmin, StringComparison.OrdinalIgnoreCase)
                && (!isActive || !string.Equals(role, AuthSessionExtensions.RoleAdmin, StringComparison.OrdinalIgnoreCase)))
            {
                var otherAdmins = _users.Values.Count(x =>
                    x.Id != target.Id
                    && x.IsActive
                    && string.Equals(x.Role, AuthSessionExtensions.RoleAdmin, StringComparison.OrdinalIgnoreCase));
                if (otherAdmins == 0)
                {
                    error = "Hệ thống phải luôn có ít nhất một admin đang hoạt động.";
                    return false;
                }
            }

            target.FullName = fullName;
            target.Role = role;
            target.IsActive = isActive;
            if (!string.IsNullOrWhiteSpace(newPassword))
                target.Password = newPassword;
        }

        error = "";
        return true;
    }

    public bool AdminDeleteUser(string id, out string error)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            error = "Thiếu định danh người dùng.";
            return false;
        }

        lock (_lock)
        {
            var target = _users.Values.FirstOrDefault(x => x.Id == id);
            if (target is null)
            {
                error = "Không tìm thấy người dùng.";
                return false;
            }

            if (string.Equals(target.Role, AuthSessionExtensions.RoleAdmin, StringComparison.OrdinalIgnoreCase))
            {
                var adminCount = _users.Values.Count(x =>
                    string.Equals(x.Role, AuthSessionExtensions.RoleAdmin, StringComparison.OrdinalIgnoreCase));
                if (adminCount <= 1)
                {
                    error = "Không thể xóa admin cuối cùng.";
                    return false;
                }
            }

            _users.Remove(target.Email);
        }

        error = "";
        return true;
    }

    public bool UpdateProfile(
        string email,
        string fullName,
        string phone,
        int? age,
        string bio,
        out UserRecord? updated,
        out string error)
    {
        email = (email ?? "").Trim();
        fullName = (fullName ?? "").Trim();
        phone = (phone ?? "").Trim();
        bio = (bio ?? "").Trim();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            error = "Họ tên không được để trống.";
            updated = null;
            return false;
        }

        if (age is < 0 or > 120)
        {
            error = "Tuổi không hợp lệ.";
            updated = null;
            return false;
        }

        lock (_lock)
        {
            if (!_users.TryGetValue(email, out var target))
            {
                error = "Không tìm thấy người dùng.";
                updated = null;
                return false;
            }

            target.FullName = fullName;
            target.Phone = phone;
            target.Age = age;
            target.Bio = bio;
            updated = Clone(target);
        }

        error = "";
        return true;
    }

    private static string NormalizeRole(string role) =>
        string.Equals(role, AuthSessionExtensions.RoleAdmin, StringComparison.OrdinalIgnoreCase)
            ? AuthSessionExtensions.RoleAdmin
            : AuthSessionExtensions.RoleUser;

    private static UserRecord Clone(UserRecord x) =>
        new()
        {
            Id = x.Id,
            Email = x.Email,
            FullName = x.FullName,
            Password = x.Password,
            Role = x.Role,
            IsActive = x.IsActive,
            CreatedAt = x.CreatedAt,
            LastLoginAt = x.LastLoginAt,
            Phone = x.Phone,
            Age = x.Age,
            Bio = x.Bio,
        };

    public sealed class UserRecord
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Password { get; set; } = "";
        public string Role { get; set; } = AuthSessionExtensions.RoleUser;
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastLoginAt { get; set; }
        public string Phone { get; set; } = "";
        public int? Age { get; set; }
        public string Bio { get; set; } = "";
    }
}
