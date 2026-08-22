using AutoMapper;
using ElectroPi.Application.Dtos.Auth;
using ElectroPi.Application.Dtos.Password;
using ElectroPi.Application.Interfaces;
using ElectroPi.Domain.Entities;
using ElectroPi.Infrastructure.Persistance;
using Microsoft.AspNetCore.Identity;
using System.Web;

namespace Infrastructure.Identity
{
    public class PasswordService : IPasswordService
    {
        private readonly AppDbContext _dbContext;
        //private readonly MessageQueueService _notificationService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;
        public PasswordService(AppDbContext dbContext, 
            //MessageQueueService notificationService, 
            UserManager<AppUser> userManager, 
            IMapper mapper)
        {
            _dbContext = dbContext;
            //_notificationService = notificationService;
            _userManager = userManager;
            _mapper = mapper;
        }

        //public async Task ResetPasswordAsync(ResetPasswordDTO resetPasswordDTO)
        //{
        //    var user = await _userManager.FindByIdAsync(resetPasswordDTO.UserId)
        //        ?? throw new KeyNotFoundException("لم يتم العثور على المستخدم");

        //    using var auditScope = AuditContext.BeginScope(action: "Reset Password", performedBy: user.Email);
        //    var result = await _userManager.ResetPasswordAsync(user, resetPasswordDTO.Token, resetPasswordDTO.NewPassword);
        //    if (!result.Succeeded)
        //        throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        //}

        //public async Task<string> RequestResetPasswordByAdminAsync(string userId)
        //{
        //    var user = await _userManager.FindByIdAsync(userId)
        //        ?? throw new KeyNotFoundException("لم يتم العثور على المستخدم");
        //    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        //    var resetLink = $"https://internal.theminaretagency.com/reset-password?userId={user.Id}&token={HttpUtility.UrlEncode(token)}";
        //    if (!string.IsNullOrEmpty(user.Email))
        //    {
        //        Dictionary<string, string> reps = new Dictionary<string, string>
        //        {
        //            { "EmployeeName", $"{user.FirstName} {user.LastName}" },
        //            { "EmployeeEmail", user.Email },
        //            { "TimeStamp", $"{DateTime.UtcNow}" },
        //            { "ResetLink", resetLink }
        //        };
        //        await _notificationService.PublishNotificationAsync(
        //        user.Email, "Reset Your Password",
        //        "RequestResetPassword",
        //        reps, "Reset Password");

        //        await _dbContext.SaveChangesAsync();
        //    }
        //    return resetLink;
        //}

        public async Task<UserDto> ChangePasswordAsync(ChangePasswordDto passwordDTO)
        {
            var user = await _userManager.FindByIdAsync(passwordDTO.Id)
                ?? throw new KeyNotFoundException("لم يتم العثور على المستخدم");

            var result = await _userManager.ChangePasswordAsync(user, passwordDTO.OldPassword, passwordDTO.NewPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException();

            if (!string.IsNullOrEmpty(user.Email))
            {
                Dictionary<string, string> reps = new Dictionary<string, string>
                {
                    {"EmpFullName", user.FullName },
                    {"EmpEmail", $"{user.Email}" },
                    {"TimeStamp", $"{DateTime.UtcNow}" }
                };
                //await _notificationService.PublishNotificationAsync(
                //    user.Email,
                //    "Change Password Confirmation",
                //    "ChangePasswordConfirmation",
                //    reps, "Change Password");

                await _dbContext.SaveChangesAsync();
            }

            return _mapper.Map<UserDto>(user);
        }
    }
}
