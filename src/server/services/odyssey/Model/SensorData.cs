using System;

namespace Odyssey.API.Model
{
    public class SensorData
    {
        public DataType DataType { get; set; }
        public int Id { get; internal set; }
        public string Key { get; set; }

        public string Value { get; set; }
        public DateTime Timespan { get; set; }
        public string SensorId { get; internal set; }

        public SensorData()
        {
            this.Timespan = DateTime.UtcNow;
        }
    }

    public enum DataType
    {
        Light = 0,
        Humidity = 1,
        Unknown = 2,
        Temperature = 3
    }
}
