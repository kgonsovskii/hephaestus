using System.Collections;
using System.Reflection;
using System.Text.Json.Serialization;

namespace model;

public abstract class BaseModel
{
    [JsonIgnore]
    protected BaseModel? Parent { get; set; }

    protected ServerModel? ServerModel
    {
        get
        {
            var model = Parent;
            while (model != null)
            {
                if (model is ServerModel serverModel)
                    return serverModel;
                model = model.Parent;
            }
            return new ServerModel();
        }
    }

    public BaseModel(BaseModel? baseModel)
    {
        Parent = baseModel;
        Refresh();
    }

    public BaseModel()
    {
    }

    protected abstract void InternalRefresh();

    public void Refresh()
    {
        InternalRefresh();
        var props = GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0).ToList();

        foreach (var prop in props)
        {
            var value = prop.GetValue(this);
            if (value is BaseModel baseModel)
            {
                baseModel.Parent = this;
                baseModel.Refresh();
            }
            else if (value is IEnumerable list && prop.PropertyType.IsGenericType &&
                     prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = prop.PropertyType.GetGenericArguments()[0];
                if (typeof(BaseModel).IsAssignableFrom(elementType))
                {
                    foreach (var item in list)
                    {
                        if (item is BaseModel itemModel)
                        {
                            itemModel.Parent = this;
                            itemModel.Refresh();
                        }
                    }
                }
            }
        }
        InternalRefresh();
    }
}