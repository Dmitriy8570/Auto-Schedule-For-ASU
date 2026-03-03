<script setup lang="ts">
import { Calendar } from 'lucide-vue-next'
</script>

<template>
    <header class="header-container">
        <div class="logo-whitebox">
        <Calendar :size="32" color="#1e40af" :stroke-width="1.5" />
        </div>

        <h1 class="main-title">Система расписаний АлтГУ</h1>
    </header>
</template>

<style scoped>
    .header-container{
        display: flex;
        flex-direction: column;
        align-items: center;
        margin-top: 40px;
    }

    .logo-whitebox{
        background-color: white;
        padding: 12px;
        border-radius: 16px;
        margin-bottom: 20px;
        display: flex;
        justify-content: center;
        align-items: center;
        /* box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1) */
    }

    .main-title {
        color: white;
        font-size: 28px;
        margin: 0;
        }

        .sub-title {
        color: rgba(255, 255, 255, 0.8);
        font-size: 14px;
    }
</style>