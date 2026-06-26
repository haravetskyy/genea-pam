namespace GeneaPam.Api.Features.Persons.Delete;

public sealed record DeletePersonCommand(Guid Id, Guid TreeId, string OwnerId);
