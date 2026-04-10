using MediatR;
using System.Threading;
using System.Threading.Tasks;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Features.Auth.Commands;

public record LoginCommand(LoginDto Dto) : IRequest<AuthResponseDto>;

public class LoginCommandHandler(IAuthService authService) : IRequestHandler<LoginCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken ct)
    {
        // Sử dụng IAuthService hiện tại như một Infrastructure Service, nhưng bọc trong Mediator Command
        return await authService.LoginAsync(request.Dto, ct);
    }
}
