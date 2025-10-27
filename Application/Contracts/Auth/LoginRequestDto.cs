using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts.Auth
{
    public sealed class LoginRequestDto
    {
        // Email ya da Username gelebilir
        public string Login { get; set; } = default!;
        public string Password { get; set; } = default!;
    }
}
