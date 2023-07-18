using Microsoft.Extensions.Options;
using OptiDataProfiles;

namespace DeaneBarker.Optimizely
{
    public class OptiDataProfile
    {
        private readonly IOptiDataProfileService _dataProfileService;

        private readonly OptiDataProfileOptions _options;

        public OptiDataProfile(IOptiDataProfileService dataProfileService, IOptions<OptiDataProfileOptions> options)
        {
            _dataProfileService = dataProfileService;
            _options = options.Value;
        }

        public OptiDataProfile(string id)
        {
            Id = id;

            try
            {
                var result = _dataProfileService.Get(id);
                if (result.Keys.Any())
                {
                    Attributes = result;
                }
            }
            catch (Exception e)
            {
                Attributes[_options.KeyField] = Id;
                CreateUser();
            }
        }

        public static Func<string> IdProvider { get; set; }

        // This is the local cache for the data profile attributes
        // They are all loaded here when the object is instantiated
        // Any attribute retrieval will be from this local cache
        public Dictionary<string, object> Attributes { get; init; } = new();

        public string Id { get; set; }

        public object GetValue(string key, object defaultValue = null)
        {
            var value = Attributes.GetValueOrDefault(key);
            return value ?? defaultValue;
        }

        public string GetString(string key, string defaultValue = null)
        {
            var value = GetValue(key)?.ToString();
            return !string.IsNullOrWhiteSpace(value) ? value : defaultValue;
        }

        public string this[string key]
        {
            get { return GetString(key); }
        }

        public void SetValue(string key, object value)
        {
            var valuesToSet = new Dictionary<string, object>()
            {
                { key, value }
            };

            SetValues(valuesToSet);
        }

        public void SetValues(object values)
        {
            var valuesToSet = new Dictionary<string, object>();
            foreach (var prop in values.GetType().GetProperties())
            {
                valuesToSet.Add(prop.Name, prop.GetValue(values, null));
            }

            SetValues(valuesToSet);
        }

        // This is the core method -- this is what every other "SetValue(s)" method calls
        public void SetValues(Dictionary<string, object> values)
        {
            // Update local cache
            foreach (var pair in values)
            {
                Attributes[pair.Key] = pair.Value;
            }

            // Make sure the key field is present
            // We can't update ODP without a key
            if (!values.ContainsKey(_options.KeyField))
            {
                values.Add(_options.KeyField, Id);
            }

            // Update
            var response = _dataProfileService.Update(values);

            if (((int)response.StatusCode).ToString()[0] != '2')
            {
                throw new Exception($"Service returned an error: {response.Content.ReadAsStringAsync().Result}");
            }
        }

        public void CreateUser()
        {
            // Passing null values which just create a new user
            // Remember, the key field and ID are always added, and that's enough to create the user
            SetValue(null, null);
        }

        public static OptiDataProfile GetForCurrentUser()
        {
            if (IdProvider == null)
            {
                throw new ArgumentNullException("IdProvider must be defined");
            }

            var id = IdProvider();
            if (id == null)
            {
                return null;
            }

            return new OptiDataProfile(id);
        }
    }
}