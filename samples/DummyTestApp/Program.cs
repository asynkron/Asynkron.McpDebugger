using Asynkron.McpDebugger.Client;

Console.WriteLine("=== Dummy Test App for MCP Debugger ===");
Console.WriteLine("Make sure 'mcpdebugger serve' is running!");
Console.WriteLine();

var app = new Application();
await app.RunAsync();

public class Application
{
    private readonly UserService _userService = new();
    private readonly OrderService _orderService = new();
    private readonly NotificationService _notificationService = new();

    public async Task RunAsync()
    {
        Console.WriteLine("[App] Starting application...");
        await DebugBreak.HereAsync(); // Breakpoint: Application startup

        // Simulate user registration flow
        var user = await _userService.RegisterUserAsync("john@example.com", "John Doe");

        // Simulate order creation
        var order = await _orderService.CreateOrderAsync(user.Id, new[] { "Widget A", "Widget B" });

        // Simulate order processing
        await _orderService.ProcessOrderAsync(order.Id);

        // Send notification
        await _notificationService.SendOrderConfirmationAsync(user.Email, order.Id);

        Console.WriteLine("[App] Application completed successfully!");
        await DebugBreak.HereAsync(); // Breakpoint: Application complete
    }
}

public class UserService
{
    private int _nextId = 1;

    public async Task<User> RegisterUserAsync(string email, string name)
    {
        Console.WriteLine($"[UserService] Registering user: {email}");
        await DebugBreak.HereAsync(); // Breakpoint: Before user registration

        // Simulate validation
        if (string.IsNullOrEmpty(email))
            throw new ArgumentException("Email is required");

        await Task.Delay(100); // Simulate async work

        var user = new User
        {
            Id = _nextId++,
            Email = email,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };

        Console.WriteLine($"[UserService] User registered with ID: {user.Id}");
        await DebugBreak.HereAsync(); // Breakpoint: After user registration

        return user;
    }

    public async Task<User?> GetUserAsync(int id)
    {
        await DebugBreak.HereAsync(); // Breakpoint: User lookup
        await Task.Delay(50);
        return null; // Simplified
    }
}

public class OrderService
{
    private int _nextOrderId = 1000;
    private readonly Dictionary<int, Order> _orders = new();

    public async Task<Order> CreateOrderAsync(int userId, string[] items)
    {
        Console.WriteLine($"[OrderService] Creating order for user {userId}");
        await DebugBreak.HereAsync(); // Breakpoint: Before order creation

        var order = new Order
        {
            Id = _nextOrderId++,
            UserId = userId,
            Items = items.ToList(),
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _orders[order.Id] = order;

        // Calculate total (simulate complex logic)
        order.Total = await CalculateTotalAsync(items);

        Console.WriteLine($"[OrderService] Order {order.Id} created with total: ${order.Total}");
        await DebugBreak.HereAsync(); // Breakpoint: After order creation

        return order;
    }

    public async Task ProcessOrderAsync(int orderId)
    {
        Console.WriteLine($"[OrderService] Processing order {orderId}");

        if (!_orders.TryGetValue(orderId, out var order))
            throw new InvalidOperationException($"Order {orderId} not found");

        await DebugBreak.HereAsync(); // Breakpoint: Before order processing

        // Step 1: Validate inventory
        Console.WriteLine($"[OrderService] Validating inventory...");
        await ValidateInventoryAsync(order.Items);

        await DebugBreak.HereAsync(); // Breakpoint: After inventory validation

        // Step 2: Process payment
        Console.WriteLine($"[OrderService] Processing payment...");
        await ProcessPaymentAsync(order.Total);

        await DebugBreak.HereAsync(); // Breakpoint: After payment processing

        // Step 3: Update status
        order.Status = OrderStatus.Completed;
        order.CompletedAt = DateTime.UtcNow;

        Console.WriteLine($"[OrderService] Order {orderId} completed!");
    }

    private async Task<decimal> CalculateTotalAsync(string[] items)
    {
        await DebugBreak.HereAsync(); // Breakpoint: Inside calculation

        await Task.Delay(50);

        // Simplified pricing
        decimal total = 0;
        foreach (var item in items)
        {
            total += item.Length * 10; // Dummy calculation
        }

        return total;
    }

    private async Task ValidateInventoryAsync(List<string> items)
    {
        foreach (var item in items)
        {
            await DebugBreak.HereAsync(); // Breakpoint: Checking each item
            Console.WriteLine($"[OrderService]   Checking inventory for: {item}");
            await Task.Delay(30);
        }
    }

    private async Task ProcessPaymentAsync(decimal amount)
    {
        await DebugBreak.HereAsync(); // Breakpoint: Payment processing
        Console.WriteLine($"[OrderService]   Charging ${amount}...");
        await Task.Delay(100);
    }
}

public class NotificationService
{
    public async Task SendOrderConfirmationAsync(string email, int orderId)
    {
        Console.WriteLine($"[NotificationService] Sending confirmation to {email}");
        await DebugBreak.HereAsync(); // Breakpoint: Before sending notification

        // Simulate sending email
        await Task.Delay(50);

        Console.WriteLine($"[NotificationService] Email sent for order {orderId}");
        await DebugBreak.HereAsync(); // Breakpoint: After sending notification
    }

    public async Task SendBatchNotificationsAsync(string[] emails, string message)
    {
        Console.WriteLine($"[NotificationService] Sending batch notifications to {emails.Length} recipients");

        foreach (var email in emails)
        {
            await DebugBreak.HereAsync(); // Breakpoint: Each notification in batch
            Console.WriteLine($"[NotificationService]   Sending to {email}...");
            await Task.Delay(20);
        }
    }
}

// Domain models
public class User
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public List<string> Items { get; set; } = new();
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum OrderStatus
{
    Pending,
    Processing,
    Completed,
    Cancelled
}
