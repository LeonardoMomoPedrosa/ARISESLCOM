using ARISESLCOM.Models.Entities;
using ARISESLCOM.Models.Mappers.interfaces;

namespace ARISESLCOM.Models.Mappers
{
    public class ProductViewMapper : IProductViewMapper
    {
        public ProductModel MapProductModel(ProductViewModel productViewModel)
        {
            return new ProductModel()
            {
                NoPac = productViewModel.NoPac != null && productViewModel.NoPac.Equals("on"),
                Promocao = productViewModel.Promocao != null && productViewModel.Promocao.Equals("on"),
                Recomenda = productViewModel.Recomenda != null && productViewModel.Recomenda.Equals("on"),
                Ativo = productViewModel.Ativo != null && productViewModel.Ativo.Equals("on"),
                Estoque = productViewModel.Estoque != null && productViewModel.Estoque.Equals("on"),
                Descricao = productViewModel.Descricao,
                DiasDisp = productViewModel.DiasDisp,
                DisplayOrder = productViewModel.DisplayOrder,
                IdFornecedor = productViewModel.IdFornecedor,
                Lucro = productViewModel.Lucro,
                MetaKey = productViewModel.MetaKey,
                MetaNome = productViewModel.MetaNome,
                MinParcJuros = productViewModel.MinParcJuros,
                Nome = productViewModel.Nome,
                Peso = productViewModel.Peso,
                PKId = productViewModel.PKId,
                PrecoAnt = productViewModel.PrecoAnt,
                PrecoAtacado = productViewModel.PrecoAtacado,
                SubTipo = productViewModel.SubTipo,
                Tipo = productViewModel.Tipo,
                Video = productViewModel.Video,
                EPS6Id = productViewModel.ERPId,
                EPS6StockMin = productViewModel.ERPStockMin,
                NomeFoto = productViewModel.NomeFoto
            };
        }

        public ProductViewModel MapProductViewModel(ProductModel productModel)
        {
            return new ProductViewModel()
            {
                Ativo = productModel.Ativo ? "true" : "false",
                NoPac = productModel.NoPac ? "true" : "false",
                Estoque = productModel.Estoque ? "true" : "false",
                Promocao = productModel.Promocao ? "true" : "false",
                Recomenda = productModel.Recomenda ? "true" : "false",
                /*Ativo = productModel.Ativo,
                NoPac = productModel.NoPac,
                Estoque = productModel.Estoque,
                Promocao = productModel.Promocao,
                Recomenda = productModel.Recomenda,*/
                Descricao = productModel.Descricao,
                DiasDisp = productModel.DiasDisp,
                DisplayOrder = productModel.DisplayOrder,
                IdFornecedor = productModel.IdFornecedor,
                Lucro = productModel.Lucro,
                MetaKey = productModel.MetaKey,
                MetaNome = productModel.MetaNome,
                MinParcJuros = productModel.MinParcJuros,
                Nome = productModel.Nome,
                Peso = productModel.Peso,
                PKId = productModel.PKId,
                PrecoAnt = productModel.PrecoAnt,
                PrecoAtacado = productModel.PrecoAtacado,
                SubTipo = productModel.SubTipo,
                Tipo = productModel.Tipo,
                Video = productModel.Video,
                ERPId = productModel.EPS6Id,
                ERPStockMin = productModel.EPS6StockMin,
                NomeFoto = productModel.NomeFoto
            };
        }

        public List<ProductViewModel> MapProductViewModelList(List<ProductModel> productModelList)
        {
            List<ProductViewModel> list = productModelList.ConvertAll(x => MapProductViewModel(x));
            return list;
        }
    }
}
