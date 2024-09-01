$(document).ready(function () {
    let table = $('#Table').DataTable({
        "language": {
            "url": "https://cdn.datatables.net/plug-ins/2.0.3/i18n/es-ES.json"
        }
    });
});

$(document).ready(function () {
    let table = $('#Tablepedido').DataTable({
        layout: {
            topStart: {
                buttons: [
                    {
                        extend: 'pdfHtml5',
                        download: 'open'
                    }
                ]
            }
        },
        "language": {
            "url": "https://cdn.datatables.net/plug-ins/2.0.3/i18n/es-ES.json"
        }
    });
});
