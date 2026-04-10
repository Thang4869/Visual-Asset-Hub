using MediatR;
using System.Threading;
using System.Threading.Tasks;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Features.Auth.Commands;

public record RegisterCommand(RegisterDto Dto) : IRequest<AuthResponseDto>;

public class RegisterCommandHandler(IAuthService authService) : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken ct)
    {
        // 1. Uỷ quyền cho Service core thực hiện Validation & EF Core Transaction
        var result = await authService.RegisterAsync(request.Dto, ct);
        
        // 2. Nâng cấp: Theo chuẩn Specs, ngay khi User đăng ký, Service Cần sinh Default "My Collection" cho User.
        // Có thể Notify qua Message Bus hoặc gọi ICollectionService ở đây nếu Cấu hình.
        
        return result;
    }
}
