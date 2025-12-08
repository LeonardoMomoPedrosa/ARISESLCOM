using ARISESLCOM.Models.Entities;

namespace ARISESLCOM.Models.Mappers
{
    public class DestaqueViewMapper
    {
        public DestaqueModel MapDestaqueModel(DestaqueViewModel viewModel)
        {
            return new DestaqueModel()
            {
                PKId = viewModel.PKId,
                Tipo = viewModel.Tipo,
                Arquivo = viewModel.Arquivo,
                Link = viewModel.Link,
                Frequencia3 = viewModel.Frequencia3
            };
        }

        public DestaqueViewModel MapDestaqueViewModel(DestaqueModel model, string? imageBaseUrl = null)
        {
            return new DestaqueViewModel()
            {
                PKId = model.PKId,
                Tipo = model.Tipo,
                Arquivo = model.Arquivo,
                Link = model.Link,
                Frequencia3 = model.Frequencia3,
                ImageUrl = !string.IsNullOrEmpty(imageBaseUrl) && !string.IsNullOrEmpty(model.Arquivo) 
                    ? $"{imageBaseUrl.TrimEnd('/')}/{model.Arquivo}" 
                    : null
            };
        }

        public List<DestaqueViewModel> MapDestaqueViewModelList(List<DestaqueModel> modelList, string? imageBaseUrl = null)
        {
            return modelList.ConvertAll(x => MapDestaqueViewModel(x, imageBaseUrl));
        }
    }
}
