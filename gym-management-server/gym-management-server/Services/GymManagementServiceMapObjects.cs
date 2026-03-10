namespace gym_management_server.Services
{
    public class GymManagementServiceMapObjects
    {
        public TDestination MapObjects<TSource, TDestination>(TSource source)
            where TDestination : new()
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), "Source cannot be null");

            var destination = new TDestination();

            var sourceProps = typeof(TSource).GetProperties()
                .Where(p => p.CanRead);

            var destProps = typeof(TDestination).GetProperties()
                .Where(p => p.CanWrite)
                .ToDictionary(p => p.Name);

            foreach (var sourceProp in sourceProps)
            {
                if (destProps.TryGetValue(sourceProp.Name, out var targetProp) &&
                    targetProp.PropertyType == sourceProp.PropertyType)
                {
                    targetProp.SetValue(destination, sourceProp.GetValue(source));
                }
            }

            return destination;
        }


    }
}
