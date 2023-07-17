using System.Runtime.InteropServices.ObjectiveC;
using System.Text.Json;

namespace DeaneBarker.Optimizely
{

	public class OptiDataProfile : IDisposable
	{
		public static string KeyField { get; set; }
		public static string ApiKey { get; set; }
		public static string ServiceUrl { get; set;  } = "https://api.zaius.com/v3/profiles";
		public static Func<string> IdProvider { get; set; }


		// This will get created and configured one time per instance
		// (This might be sub-optimal. I might not understand HttpClient very well.)
		private HttpClient client = null;

		// This is the local cache for the data profile attributes
		// They are all loaded here when the object is instantiated
		// Any attribute retrieval will be from this local cache
		public Dictionary<string, object> Attributes { get; init; } = new();

		public string Id { get; set; }

		public OptiDataProfile(string id)
		{
			EnsureInit();

			Id = id;

			try
			{
				var result = client.GetStringAsync($"https://api.zaius.com/v3/profiles?{KeyField}={id}").Result;
				var jsonObj = JsonSerializer.Deserialize<JsonElement>(result);

                foreach (var pair in jsonObj.GetProperty("attributes").EnumerateObject())
				{
					Attributes.Add(pair.Name, pair.Value.ToString());
				}
			}
			catch(Exception e)
			{
				Attributes[KeyField] = Id;
				CreateUser();
			}
		}

		public object GetValue(string key, object defaultValue = null)
		{
			EnsureInit();

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
			EnsureInit();

			// Update local cache
			foreach(var pair in values)
			{
				Attributes[pair.Key] = pair.Value;
			}

			// Make sure the key field is present
			// We can't update ODP without a key
			if(!values.ContainsKey(KeyField))
			{
				values.Add(KeyField, Id);
			}

			// Update
            var json = JsonContent.Create(new { attributes = values });
            var response = client.PostAsync(ServiceUrl, json).Result;

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

		private void EnsureInit()
		{
			if (string.IsNullOrWhiteSpace(KeyField))
			{
				throw new Exception("You must set a KeyField value");
			}

			if (string.IsNullOrWhiteSpace(ApiKey))
			{
				throw new Exception("You must set an ApiKey value");
			}

			if (client == null)
			{
				client = new HttpClient();
				client.DefaultRequestHeaders.Add("x-api-key", ApiKey);
			}
        }

		public static OptiDataProfile GetForCurrentUser()
		{
            if (IdProvider == null)
            {
                throw new ArgumentNullException("IdProvider must be defined");
            }

			var id = IdProvider();
			if(id == null)
			{
				return null;
			}

			return new OptiDataProfile(id);
		}
	}
  	public void Dispose()
    	{
        	client.Dispose();
    	}
}
