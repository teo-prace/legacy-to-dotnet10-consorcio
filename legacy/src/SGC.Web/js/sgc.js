/* SGC - funcoes de tela (2011) */

function somenteNumeros(e) {
    var tecla = (window.event) ? event.keyCode : e.which;
    if (tecla == 8 || tecla == 0) return true;
    return (tecla >= 48 && tecla <= 57);
}

function mascaraCPF(campo) {
    var v = campo.value.replace(/\D/g, '');
    if (v.length > 11) v = v.substring(0, 11);
    v = v.replace(/(\d{3})(\d)/, '$1.$2');
    v = v.replace(/(\d{3})(\d)/, '$1.$2');
    v = v.replace(/(\d{3})(\d{1,2})$/, '$1-$2');
    campo.value = v;
}

function mascaraData(campo) {
    var v = campo.value.replace(/\D/g, '');
    if (v.length > 8) v = v.substring(0, 8);
    if (v.length > 4) v = v.substring(0, 2) + '/' + v.substring(2, 4) + '/' + v.substring(4);
    else if (v.length > 2) v = v.substring(0, 2) + '/' + v.substring(2);
    campo.value = v;
}

function validaCPF(cpf) {
    cpf = cpf.replace(/\D/g, '');
    if (cpf.length != 11) return false;
    var soma = 0, i, resto;
    for (i = 0; i < 9; i++) soma += parseInt(cpf.charAt(i)) * (10 - i);
    resto = 11 - (soma % 11);
    if (resto >= 10) resto = 0;
    if (resto != parseInt(cpf.charAt(9))) return false;
    soma = 0;
    for (i = 0; i < 10; i++) soma += parseInt(cpf.charAt(i)) * (11 - i);
    resto = 11 - (soma % 11);
    if (resto >= 10) resto = 0;
    return resto == parseInt(cpf.charAt(10));
}

function validaCPFCampo(source, args) {
    args.IsValid = validaCPF(args.Value);
}

function confirma(msg) {
    return confirm(msg);
}
