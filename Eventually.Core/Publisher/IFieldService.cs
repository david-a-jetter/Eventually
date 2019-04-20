using Eventually.Core.Publisher.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Eventually.Core.Publisher
{
    public interface IFieldService
    {
        Task<bool> AnnotateField(long fieldId, Annotation annotation);
        Task<IReadOnlyCollection<FirstClassField>> GetUnannotatedFields();
    }
}
