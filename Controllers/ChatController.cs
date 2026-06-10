using Microsoft.AspNetCore.Mvc;
using AspNetMvcApp.Services;

namespace AspNetMvcApp.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChatController : ControllerBase
{
    private readonly IChatbotService _chatbotService;

    public ChatController(IChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message cannot be empty.");
        }

        var response = await _chatbotService.GetResponseAsync(request.Message);
        return Ok(new { Reply = response });
    }
}
