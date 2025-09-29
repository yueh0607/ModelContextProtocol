using System.Threading;
using System.Threading.Tasks;
using McpServerLib.Mcp.Attributes;

namespace McpServerLib.Mcp.Tools
{
    [McpToolClass(Category = "Basic", Description = "Basic utility tools")]
    public class BasicTools
    {
        [McpTool("echo", Description = "Echoes back the provided message")]
        public string Echo(
            [McpParameter("The message to echo back")] string message)
        {
            return $"Echo: {message}";
        }

        [McpTool("add", Description = "Adds two numbers")]
        public double Add(
            [McpParameter("First number")] double a,
            [McpParameter("Second number")] double b)
        {
            return a + b;
        }

        [McpTool("multiply", Description = "Multiplies two numbers")]
        public async Task<double> MultiplyAsync(
            [McpParameter("First number")] double a,
            [McpParameter("Second number")] double b,
            CancellationToken cancellationToken = default)
        {
            // Simulate some async work
            await Task.Delay(10, cancellationToken);
            return a * b;
        }

        [McpTool("greet", Description = "Greets a person")]
        public string Greet(
            [McpParameter("Name of the person to greet")] string name,
            [McpParameter("Title for the person", Required = false)] string title = null)
        {
            if (!string.IsNullOrEmpty(title))
                return $"Hello, {title} {name}!";
            return $"Hello, {name}!";
        }
    }
}