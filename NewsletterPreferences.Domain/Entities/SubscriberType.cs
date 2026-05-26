using NewsletterPreferences.Domain.Common;

namespace NewsletterPreferences.Domain.Entities;

public class SubscriberType : Entity
{
    public new int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;

    private SubscriberType() { }

    public static SubscriberType Create(int id, string name, string code) =>
        new() { Id = id, Name = name, Code = code };
}
