import { createApp } from 'vue'
import './style.css'
import ui from '@nuxt/ui/vue-plugin'
import App from './App.vue'
import { createPinia } from 'pinia'

import { router } from './routes'

const app = createApp(App)
const pinia = createPinia()

app.use(router)
app.use(ui)
app.use(pinia)

app.mount('#app')
