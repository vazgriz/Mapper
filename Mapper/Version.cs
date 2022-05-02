using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Mapper {
    public class ToStringJsonConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return true;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            writer.WriteValue(value.ToString());
        }

        public override bool CanRead {
            get { return true; }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            if (objectType != typeof(Version)) {
                throw new ArgumentException(nameof(objectType));
            }

            if (reader.TokenType == JsonToken.Null) {
                return new Version(0, 0);
            }

            if (reader.TokenType == JsonToken.String) {
                var s = (string)reader.Value;
                return Version.Parse(s);
            }

            throw new ArgumentException("Unsupported");
        }
    }

    [JsonConverter(typeof(ToStringJsonConverter))]
    public class Version {
        public int Major { get; set; }
        public int Minor { get; set; }

        public Version(int major, int minor) {
            Major = major;
            Minor = minor;
        }

        public override string ToString() {
            return string.Format("{0}.{1}", Major, Minor);
        }

        public static bool operator ==(Version a, Version b) {
            return a.Major == b.Major && a.Minor == b.Minor;
        }

        public static bool operator !=(Version a, Version b) {
            return a.Major != b.Major || a.Minor != b.Minor;
        }

        public override bool Equals(object obj) {
            if (obj is Version other) {
                return this == other;
            }

            return false;
        }

        public override int GetHashCode() {
            return Major.GetHashCode() ^ Minor.GetHashCode();
        }

        public static Version Parse(string value) {
            if (string.IsNullOrEmpty(value)) {
                throw new ArgumentException(nameof(value));
            }

            var tokens = value.Split('.');
            if (tokens.Length != 2) {
                throw new ArgumentException(string.Format("Invalid Version string. Expected Major.Minor, got {0}", value));
            }

            int major = 0;
            int minor = 0;

            bool valid = int.TryParse(tokens[0], out major);
            valid = valid && int.TryParse(tokens[1], out minor);

            if (!valid) {
                throw new ArgumentException(string.Format("Invalid Version string. Expected Major.Minor, got {0}", value));
            }

            return new Version(major, minor);
        }
    }
}
