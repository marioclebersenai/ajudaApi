[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    public AuthController(ApplicationDbContext context, IConfiguration configuration, IPasswordHasher<Usuario> passwordHasher)
    {
        _context = context;
        _configuration = configuration;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = new Usuario
        {
            Nome = model.Nome,
            Email = model.Email,
            Role = model.Role
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

        _context.Usuarios.Add(user);
        await _context.SaveChangesAsync();


       
        return Ok(new { Message = "Registro efetuado com sucesso!" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == model.Email);
        if (user == null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password) == PasswordVerificationResult.Failed)
        {
            return Unauthorized();
        }

        var token = GenerateJwtToken(user);
        return Ok(new { token });
    }

    private string GenerateJwtToken(Usuario user)
    {
        var claims = new[]
        {
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, user.Nome),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Role, user.Role) // Include role in the token
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpireMinutes"])),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}