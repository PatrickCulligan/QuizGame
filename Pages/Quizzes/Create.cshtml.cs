using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QuizGame.Pages.Quizzes
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        // existing code...
    }
}