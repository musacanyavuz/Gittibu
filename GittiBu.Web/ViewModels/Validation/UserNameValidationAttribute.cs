using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using GittiBu.Services;

namespace GittiBu.Web.ViewModels.Validation
{
    public class UserNameValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            using (var service = new UserService())
            {
                var usernameIsUsable = service.UsernameIsUsable(value.ToString(), 0);
                return usernameIsUsable ? ValidationResult.Success : new ValidationResult(ErrorMessage = "Geçersiz kullanıcı adı");
            }
        }
    }

    public class EmailUniqValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
            using (var service = new UserService())
            {
                var emailIsUsable = service.EmailIsUsable(value.ToString(), 0);
                return emailIsUsable ? ValidationResult.Success : new ValidationResult(ErrorMessage = "Geçersiz e-posta adresi.");
            }
        }
    }
}