using NewsletterPreferences.Domain.Common;

namespace NewsletterPreferences.Domain.Entities;

public class NewsletterInterest : Entity
{
    public new int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;

    private NewsletterInterest() { }

    public static NewsletterInterest Create(int id, string name, string code) =>
        new() { Id = id, Name = name, Code = code };
}
