using AutoMapper;
using ElectroPi.Application.Dtos.Auth;
using ElectroPi.Application.Interfaces;
using ElectroPi.Domain.Entities;
using ElectroPi.Infrastructure.Persistance;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Identity
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _dbContext;
        //private readonly MessageQueueService _notificationService;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IJwtServices _jwtServices;
        private readonly IMemoryCache _cache;
        private readonly IMapper _mapper;


        private const string CacheKey = "employee_lookup";
        public AuthService(
            AppDbContext dbContext,
            //MessageQueueService notificationService,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IJwtServices jwtServices,
            IMemoryCache cache,
            IMapper mapper
            )
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtServices = jwtServices;
            _cache = cache;
            _mapper = mapper;
        }
        public async Task<List<UserDto>> GetAllAsync()
        {
            if (_cache.TryGetValue(CacheKey, out List<UserDto>? cachedUsers))
                return cachedUsers!;

            var users = await _dbContext.Users
                //.Include(u => u.Roles)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            //foreach (var user in users)
            //{
            //    var roles = await _userManager.GetRolesAsync(user);
            //    user
            //}
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromDays(7))
                .SetSlidingExpiration(TimeSpan.FromHours(24));

            return _mapper.Map<List<UserDto>>(users);
        }

        public void Invalidate() => _cache.Remove(CacheKey);
        public async Task<UserDto> GetByIdAsync(string userId)
        {
            if (_cache.TryGetValue(CacheKey, out List<UserDto>? cachedUsers))
                return cachedUsers?.FirstOrDefault(u => u.Id == userId)!;

            var user = await _userManager
                .FindByIdAsync(userId)
                ?? throw new KeyNotFoundException();

            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Roles = roles.ToList(),
            };
        }
        public async Task<AuthResponseDto> LoginAsync(LoginDto login)
        {
            var user = await _userManager.FindByNameAsync(login.PhoneNumber.Trim());

            if (user is null)
                throw new UnauthorizedAccessException("لا يوجد حساب بهذه البيانات");

            if (user.IsActive)
                throw new UnauthorizedAccessException("لا يوجد حساب بهذه البيانات");

            var passCheck = await _userManager.CheckPasswordAsync(user, login.Password.Trim());
            if (!passCheck)
                throw new UnauthorizedAccessException("رقم الهاتف او كلمة مرور غير صحيحة");

            var roles = await _userManager.GetRolesAsync(user);
            var token = await _jwtServices.GenerateAccessTokenAsync(user);

            var authDTO = new AuthResponseDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                UserName = user.UserName,
                Roles = roles.ToList(),
                IsAuthenticated = true,
                Token = token
            };
            
            if (user.RefreshTokens.Any(u => u.IsActive))
            {
                var ActiveRefreshToken = user.RefreshTokens.First(t => t.IsActive);
                authDTO.RefreshToken = ActiveRefreshToken.Token;
                authDTO.RefreshTokenExpiration = ActiveRefreshToken.ExpiresOn;
            }
            else
            {
                var RefreshToken = await _jwtServices.GenerateRefreshTokenAsync();
                authDTO.RefreshToken = RefreshToken.Token;
                authDTO.RefreshTokenExpiration = RefreshToken.ExpiresOn;
                user.RefreshTokens.Add(RefreshToken);
                //using (AuditContext.BeginScope(suppress: true))
                //{
                //    await _userManager.UpdateAsync(user);
                //}
            }

            //await loginLogService.LogActionAsync("User", "Login", user.Id, user.Email ?? user.UserName ?? "unknown");

            authDTO.Message = "تم تسجيل الدخول بنجاح";
            return authDTO;
        }
        public async Task<UserDto> RegisterAsync(RegisterDto newUser)
        {
            var validateErrors = await ValidateRegisterAsync(newUser);
            if (validateErrors is not null && validateErrors.Count > 0)
                throw new InvalidOperationException(string.Join(", ", validateErrors));

            using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var user = new AppUser
                {
                    FullName = newUser.FullName,
                    UserName = newUser.PhoneNumber,
                    Email = newUser.Email,
                    EmailConfirmed = true,
                    PhoneNumber = newUser.PhoneNumber,
                    PhoneNumberConfirmed = true,
                };

                var result = await _userManager.CreateAsync(user, newUser.Password);

                if (!result.Succeeded)
                    throw new InvalidOperationException("Failed To Add New User");

                await _userManager.AddToRoleAsync(user, newUser.Role.ToString());

                Dictionary<string, string> reps = new Dictionary<string, string>
                {
                    {"EmpName", user.FullName },
                    {"EmpEmail", $"{user.Email}" },
                    {"EmpRole", string.Join(", ", await _userManager.GetRolesAsync(user)) }
                };
                //await _notificationService.PublishNotificationAsync(user.Email, "Welcome On Board", "EmployeeOnBoarding", reps, "New User");

                await _dbContext.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                Invalidate();
                return _mapper.Map<UserDto>(user);
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<UserDto> UpdateAsync(UpdateUserDto updatedUser)
        {
            var user = await _userManager.FindByIdAsync(updatedUser.Id)
                ?? throw new KeyNotFoundException("لم يتم العثور على المستخدم");

            if (user.FullName != updatedUser.FullName 
                && !string.IsNullOrWhiteSpace(updatedUser.FullName)) 
                user.FullName = updatedUser.FullName.Trim();

            if (user.Email != updatedUser.Email 
                && !string.IsNullOrWhiteSpace(updatedUser.Email))
            {
                await _userManager.SetEmailAsync(user, updatedUser.Email.Trim());
                await _userManager.UpdateNormalizedEmailAsync(user);

                user.EmailConfirmed = true;
            }

            //if (user.PhoneNumber != updatedUser.PhoneNumber 
            if(user.PhoneNumber != updatedUser.PhoneNumber
                && !string.IsNullOrWhiteSpace(updatedUser.PhoneNumber))
            {
                await _userManager.SetPhoneNumberAsync(user, updatedUser.PhoneNumber.Trim());
                await _userManager.SetUserNameAsync(user, updatedUser.PhoneNumber.Trim());
                user.PhoneNumberConfirmed = true;
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            // before calling userManager.UpdateAsync(user)

            // Add new primary role
            var addResult = await _userManager.AddToRoleAsync(user, updatedUser.Role.ToString());
            if (!addResult.Succeeded)
            {
                var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to add role '{updatedUser.Role.ToString()}': {errors}");
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new InvalidOperationException();

            Invalidate();
            if (!string.IsNullOrEmpty(user.Email))
            {
                Dictionary<string, string> reps = new Dictionary<string, string>
                {
                    {"EmpFullName", user.FullName },
                    {"EmpEmail", $"{user.Email}" },
                    {"TimeStamp", $"{DateTime.UtcNow}" }
                };
                //await _notificationService.PublishNotificationAsync(user.Email, "Profile Updated Successfully", "ProfileUpdate", reps, "Profile Updates");
                await _dbContext.SaveChangesAsync();
            }

            return _mapper.Map<UserDto>(user);
        }
        
        public async Task<bool> DeleteAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new KeyNotFoundException("لم يتم العثور على المستخدم");

            await _userManager.DeleteAsync(user);
            Invalidate();
            return true;
        }

        public async Task<List<string>> ValidateRegisterAsync(RegisterDto registerDTO)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(registerDTO.FullName))
                errors.Add("الاسم مطلوب");

            if (string.IsNullOrWhiteSpace(registerDTO.Email))
                errors.Add("ايميل غير صالح");

            if (await _userManager.FindByEmailAsync(registerDTO.Email) is not null)
                errors.Add("هذا الايميل موجود بالفعل");

            if (string.IsNullOrWhiteSpace(registerDTO.Password))
                errors.Add("الرقم السري مطلوب");
            else if (registerDTO.Password.Length < 6)
                errors.Add("الرقم السري يجب ان يكون 6 احرف على الاقل");

            if (string.IsNullOrWhiteSpace(registerDTO.PhoneNumber))
                errors.Add("رقم الهاتف مطلوب");

            if (await _userManager.Users.AnyAsync(u => u.PhoneNumber == registerDTO.PhoneNumber))
                errors.Add("رقم الهاتف موجود بالفعل");

            return errors;
        }

        //public async Task<List<LookUpUsers>> LookUpAllUsers()
        //{
        //    var users = await _dbContext.Users
        //        .AsNoTracking()
        //        .Select(u => new { u.Id, u.FirstName, u.LastName })
        //        .ToListAsync();

        //    List<LookUpUsers> res = new List<LookUpUsers>();
        //    foreach (var user in users)
        //    {
        //        var rec = new LookUpUsers(user.Id, user.FirstName, user.LastName);
        //        res.Add(rec);
        //    }
        //    return res;
        //}
    }
}