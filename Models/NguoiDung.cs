using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ThueVayDuTiec.Web.Models;

public class NguoiDung : IdentityUser
{
    [Required, StringLength(100)]
    public string HoTen { get; set; } = "";
}
