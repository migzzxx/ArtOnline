const field = document.getElementById('field');
const btn = document.getElementById('btn');

btn.addEventListener('click', function() {
    if (field.type === 'password') {
        field.type = 'text';
        btn.textContent = 'visibility_off';
    } else {
        field.type = 'password';
        btn.textContent = 'visibility';
    }
});