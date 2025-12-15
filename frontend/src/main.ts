import './style.css'
import App from './App.vue'
import { createApp } from 'vue'
import { router } from './routes'
import { createPinia } from 'pinia'
import ui from '@nuxt/ui/vue-plugin'
import { addCollection } from '@iconify/vue'
import lucide from '@iconify-json/lucide/icons.json'

addCollection(lucide)

const app = createApp(App)
const pinia = createPinia()

app.use(router)
app.use(ui)
app.use(pinia)

app.mount('#app')
