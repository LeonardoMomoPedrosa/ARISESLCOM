# Configuração NGINX para Upload de Arquivos

## Problema
Erro 413 (Request Entity Too Large) ao fazer upload de imagens para `/Gerencia/UploadModalEntradaImage`.

## Causa
O NGINX tem um limite padrão de tamanho de requisição (`client_max_body_size`) que geralmente é 1MB. Requisições maiores são rejeitadas antes de chegar ao Kestrel.

## Solução

### Opção 1: Configuração Global (Recomendado)
Edite o arquivo principal do NGINX (`/etc/nginx/nginx.conf`) e adicione dentro do bloco `http`:

```nginx
http {
    # ... outras configurações ...
    
    # Permitir uploads de até 20MB (mesmo limite usado no código)
    client_max_body_size 20M;
    
    # ... resto da configuração ...
}
```

### Opção 2: Configuração Específica para o Site
Se você tem um arquivo de configuração específico para o site (ex: `/etc/nginx/sites-available/novager.aquanimal.com.br`), adicione dentro do bloco `server`:

```nginx
server {
    listen 80;
    server_name novager.aquanimal.com.br;
    
    # Permitir uploads de até 20MB
    client_max_body_size 20M;
    
    location / {
        proxy_pass http://localhost:8058;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### Opção 3: Configuração Específica para Rota (Se necessário)
Se você quiser permitir uploads maiores apenas para rotas específicas:

```nginx
server {
    # ... outras configurações ...
    
    # Limite padrão menor
    client_max_body_size 1M;
    
    # Limite maior para rotas de upload
    location ~ ^/(Content|Gerencia)/(UploadCarouselImage|UploadMosaicImage|UploadModalEntradaImage) {
        client_max_body_size 20M;
        proxy_pass http://localhost:8058;
        # ... outras configurações de proxy ...
    }
    
    location / {
        proxy_pass http://localhost:8058;
        # ... outras configurações de proxy ...
    }
}
```

## Após Configurar

1. **Testar a configuração:**
   ```bash
   sudo nginx -t
   ```

2. **Recarregar o NGINX:**
   ```bash
   sudo systemctl reload nginx
   # ou
   sudo service nginx reload
   ```

3. **Verificar se funcionou:**
   - Tente fazer upload novamente
   - Verifique os logs do NGINX se ainda houver problemas

## Notas

- O limite de 20MB está alinhado com o `RequestSizeLimit(20_000_000)` configurado nos controllers
- Se você aumentar o limite no código, lembre-se de aumentar também no NGINX
- O carrossel pode estar funcionando porque usa arquivos menores, ou pode haver uma configuração específica já aplicada

