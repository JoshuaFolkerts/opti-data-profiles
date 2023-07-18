namespace OptiDataProfiles;

public interface IOptiDataProfileService
{
    Dictionary<string, object> Get(string id);

    HttpResponseMessage? Update(Dictionary<string, object> values);
}