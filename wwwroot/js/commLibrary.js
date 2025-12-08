// myLibrary.js
const commLibrary = {
    slfetch6: function (action, method, formElement, divSpin, divAlert, errorMessage) {
        this.slfetch(action, method, formElement, divSpin, divAlert, errorMessage, undefined, undefined);
    },
    uploadFile: function (action, formDiv, divSpin, divAlert, errorMessage, successCallBack) {
        const formData = new FormData();

        // percorre todos os inputs dentro da DIV de upload
        formDiv.querySelectorAll('input').forEach(input => {
            if (input.type === 'file' && input.files.length > 0) {
                formData.append(input.name, input.files[0]);
            } else if (input.type !== 'file') {
                formData.append(input.name, input.value);
            }
        });

        this.showDiv(divSpin);

        fetch(action, {
            method: 'POST',
            body: formData
        })
            .then(response => {
                this.hideDiv(divSpin);
                if (response.ok) {
                    return response.json();
                } else {
                    throw new Error(errorMessage);
                }
            })
            .then(data => {
                if (data.success) {
                    this.setDivOk(divAlert);
                    if (successCallBack) successCallBack(data);
                } else {
                    this.setDivError(divAlert);
                }
                if (divAlert) {
                    divAlert.innerText = data.message;
                    this.showFadeDiv(divAlert);
                }
            })
            .catch(error => {
                this.setDivError(divAlert);
                if (divAlert) {
                    divAlert.innerText = errorMessage;
                    this.showFadeDiv(divAlert);
                }
            });
    },
    slfetch: function (action, method, formElement, divSpin, divAlert, errorMessage, elemToHide, successCallBack) {
        const formData = new FormData(formElement);
        const jsonData = {};

        this.showDiv(divSpin);

        formData.forEach((value, key) => {
            key = key.split(".")[1];
            if (key !== undefined) {
                jsonData[key] = value;
            }
        });

        fetch(action, {
            method: method,
            body: JSON.stringify(jsonData),
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
        })
        .then(response => {
            this.hideDiv(divSpin);
            if (response.ok) {
                return response.json();
            } else {
                throw new Error(errorMessage);
            }
        })
        .then(data => {
            if (data.success) {
                this.setDivOk(divAlert);
                if (elemToHide) {
                    this.hideDiv(elemToHide);
                }
                if (successCallBack) {
                    successCallBack(data);
                }
            } else {
                this.setDivError(divAlert);
            }
            divAlert.innerText = data.message;
            this.showFadeDiv(divAlert);
        })
        .catch(error => {
            this.setDivError(divAlert);
            if (divAlert) {
                divAlert.innerText = errorMessage;
            }
            this.showFadeDiv(divAlert);
        });
    },
    showDiv: function (divv) {
        if (divv) {
            divv.classList.remove("d-none");
        }
    },
    showFadeDiv: function (divv) {
        if (divv) {
            divv.classList.remove('d-none');
            divv.classList.add('show');
            setTimeout(() => {
                divv.classList.remove('show');
            }, 1000);
        }
    },
    hideDiv: function (divv) {
        if (divv) {
            divv.classList.add("d-none");
        }
    },
    setDivError: function (divv) {
        if (divv) {
            divv.classList.remove("alert-primary");
            divv.classList.add("alert-danger");
        }
    },
    setDivOk: function (divv) {
        if (divv) {
            divv.classList.remove("alert-danger");
            divv.classList.add("alert-primary");
        }
    },
    calculateSum: function (a, b) {
        return a + b;
    }
};