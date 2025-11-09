import { createApp } from 'vue'
import './style.css'
import ui from '@nuxt/ui/vue-plugin'
import App from './App.vue'
import {router} from './routes'

const app = createApp(App)

app.use(router)
app.use(ui)

app.mount('#app')
