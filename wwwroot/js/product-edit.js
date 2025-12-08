document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.upload-btn').forEach(button => {
        button.addEventListener('click', function (event) {
            event.preventDefault();

            const formDiv = this.closest('.image-upload-form');
            if (!formDiv) {
                console.error('Form div not found');
                return;
            }
            
            const idInput = formDiv.querySelector('input[name="id"]');
            if (!idInput) {
                console.error('ID input not found');
                return;
            }
            
            const outerForm = this.closest('form.update-form');
            if (!outerForm) {
                console.error('Outer form not found');
                return;
            }
            
            // Buscar os elementos de alerta e spinner dentro do form mais próximo
            const divAlert = outerForm.querySelector('.updAlert');
            const divSpin = outerForm.querySelector('.spin');

            const action = `/Product/UploadImage?id=${idInput.value}`;

            commLibrary.uploadFile(action, formDiv, divSpin, divAlert, 'Erro ao enviar imagem', (data) => {
                if (data.success && data.fileName) {
                    const preview = formDiv.querySelector('.preview-img');
                    const fileNameDiv = formDiv.querySelector('.current-file-name');

                    const ts = new Date().getTime();
                    const id = idInput.value;
                    const baseUrl = formDiv.dataset.baseUrl || '';
                    const siteImagePath = formDiv.dataset.siteImagePath || 'images/fotosup/prodimg';
                    const newSrc = `${baseUrl}/${siteImagePath}/${id}/${data.fileName}?v=${ts}`;

                    if (preview) {
                        preview.style.display = '';
                        preview.src = newSrc;
                    }
                    if (fileNameDiv) {
                        fileNameDiv.textContent = `Atual: ${data.fileName}`;
                    }
                }
            });
        });
    });
});
