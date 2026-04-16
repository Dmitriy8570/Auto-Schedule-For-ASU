<script setup lang="ts">
import { User, Lock, Eye } from 'lucide-vue-next'
</script>

<template>
  <div class="login-card">
    <h2>Вход в систему</h2>
    
    <div class="form-group">
      <label>Имя пользователя</label>
      <div class="input-wrapper">
        <User class="icon-left" :size="18" />
        <input type="text" placeholder="Введите имя пользователя" />
      </div>
    </div>

    <div class="form-group">
      <label>Пароль</label>
      <div class="input-wrapper">
        <Lock class="icon-left" :size="18" />
        <input type="password" placeholder="Введите пароль" />
        <Eye class="icon-right" :size="18" />
      </div>
    </div>

    <button class="submit-btn">Войти</button>
</div>
</template>

<style scoped>
/* Сама карточка */
.login-card {
  background-color: white;
  width: 450px; 
  padding: 50px; 
  border-radius: 16px;
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.05);
  box-sizing: border-box; /* Важно: чтобы padding не увеличивал ширину карточки */

  display: flex;
  flex-direction: column;
  justify-content: center; /* Оставляет элементы по центру по вертикали */
  align-items: stretch;
}

h2 {
  margin: 0 0 24px 0;
  color: #1e293b;
  font-size: 22px;
  font-weight: 700;
}

/* Контейнер для каждого поля ввода с текстом над ним */
.form-group {
  width: 100%; /* Принудительно говорим занимать всё место */
  margin-bottom: 20px;
}

/* Текст "Имя пользователя" и "Пароль" */
label {
  display: block;
  font-size: 13px;
  font-weight: 600;
  color: #334155;
  margin-bottom: 8px;
}

/* Обертка для инпута, чтобы позиционировать иконки внутри */
.input-wrapper {
  position: relative;
}

/* Сами поля ввода */
input {
  width: 100%;
  height: 46px; /* Точная высота по макету */
  padding-left: 40px; /* Место для левой иконки */
  padding-right: 40px; /* Место для правой иконки */
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  font-size: 14px;
  color: #0f172a;
  outline: none; /* Убираем стандартную черную рамку при клике */
  box-sizing: border-box;
  transition: border-color 0.2s; /* Плавная смена цвета рамки */
}

input:focus {
  border-color: #1a4d9c; /* Синяя рамка при вводе текста */
}

input::placeholder {
  color: #94a3b8; /* Светлый текст подсказки */
}

/* Иконки внутри полей */
.icon-left, .icon-right {
  position: absolute;
  top: 50%;
  transform: translateY(-50%); /* Выравниваем ровно по центру по вертикали */
  color: #94a3b8;
  font-size: 16px;
}

.icon-left { left: 12px; }
.icon-right { right: 12px; cursor: pointer; }

/* Синяя кнопка */
.submit-btn {
  width: 100%;
  height: 46px;
  background-color: #1a4d9c;
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 15px;
  font-weight: 600;
  cursor: pointer;
  margin-top: 8px;
  margin-bottom: 30px; /* Отступ до демо-блока */
  transition: background-color 0.2s;
}

.submit-btn:hover {
  background-color: #143c82; /* Чуть темнеет при наведении */
}
</style>