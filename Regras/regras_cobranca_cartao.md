essa é a gerencia do e-commerce, estou com um problema.
No detalhe do pedido, /Order/ShowOrder, a somatória do pedido é Total, Retira Credito, Aplica desconto, e Frete.
no /Acr tenho a pagina de cobrança manual de cartão. Ele segue algumas regras
- Se for cartão a vista com frete, apenas um botão para cobrar Produto e cobra tudo junto. Porém mostra valor de Produtos e Frete Separado.
- Se for catão parcelado, mostra dois botões, cobrar produtos separado, especificando o número  de parcelas e cobra o frete num outro botão, em 1 parcela. 
- Se for cartão, porém frete=0, mostra o valor dos Produtos e Frete=0 porém mostra só um botão de cobrar produtos.
- Nenhum calculo de desconto, credito e parcelas precisa ser feito no /Acr, a conclusão da venda já grava os campos na tabela tbcompra: parcs é o total de parcela e parcVal é o valor da parcela. Portanto, o total a ser mostrado dos produtos é parcVal * parc. Isso facilita bastante. Esse total também deve ser usado para enviar a transação para e.Rede. 