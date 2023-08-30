using Newtonsoft.Json;

namespace EventBus.Base.Events
{
    public class IntegrationEvent
    {
        [JsonProperty]
        public Guid Id { get; private set; }

        [JsonProperty]
        public DateTime CreatedDate { get; private set; }

        public IntegrationEvent()
        {
            Id = Guid.NewGuid();
            CreatedDate = DateTime.Now;
        }


        //Json kullanılarak serialization/deserialization işlemi yapıldığında dışarıdan gelen parametreleri set edebilmek için Newtonsoft.Json altındaki JsonConstructor kullandık

        // property'leri artık parametreden alabildiğimiz için set metodunu private olarak güncelledik ki dışarıdan erişilmesin 
        [JsonConstructor]
        public IntegrationEvent(Guid id, DateTime createdDate)
        {
            Id = id;
            CreatedDate = createdDate;
        }

        
    }
}
