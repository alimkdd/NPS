using NewsletterPreferences.Domain.Common;

namespace NewsletterPreferences.Domain.Entities;

public class CommunicationPreference : Entity
{
    public new int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;

    private CommunicationPreference() { }

    public static CommunicationPreference Create(int id, string name, string code) =>
        new() { Id = id, Name = name, Code = code };
}
