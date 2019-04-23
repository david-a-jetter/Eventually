using Eventually.Core.Publisher.Models;
using System.Threading.Tasks;

namespace Eventually.Core.Publisher
{
    interface IDataEntryService
    {
        Task AnnotateField(long fieldId, Annotation annotation);
    }
}
