<script setup lang="ts">
import { ref } from "vue";
import * as z from "zod";
import type { AuthFormField, FormSubmitEvent } from "@nuxt/ui";
import { useAuthStore } from "@/stores/AuthStore";

const authStore = useAuthStore();

const fields: AuthFormField[] = [
  {
    name: "email",
    type: "email",
    label: "Email",
    placeholder: "Enter your email",
    required: true,
    autocomplete: "email",
  },
  {
    name: "password",
    type: "password",
    label: "Password",
    placeholder: "Enter your password",
    required: true,
    autocomplete: "current-password",
  },
];

const schema = z.object({
  email: z.email("Enter a valid email."),
  password: z.string().min(1, "Enter your password."),
});

type Schema = z.output<typeof schema>;

const error = ref("");
const loading = ref(false);

async function onSubmit(event: FormSubmitEvent<Schema>) {
  error.value = "";
  loading.value = true;
  try {
    await authStore.login(event.data.email, event.data.password);
    window.location.href = "/";
  } catch {
    error.value = "Invalid email or password.";
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="flex min-h-dvh items-center justify-center bg-default p-4">
    <UCard class="w-full max-w-md">
      <UAuthForm
        :schema="schema"
        :fields="fields"
        :submit="{ label: 'Sign in', block: true, loading }"
        icon="i-lucide-lock"
        title="Njeremoto Dashboard"
        description="Sign in to your account."
        @submit="onSubmit"
      >
        <template #password-hint>
          <RouterLink to="/reset-password" class="text-primary font-medium">
            Forgot password?
          </RouterLink>
        </template>
        <template #validation>
          <UAlert
            v-if="error"
            color="error"
            variant="subtle"
            icon="i-lucide-circle-alert"
            :title="error"
          />
        </template>
      </UAuthForm>
    </UCard>
  </div>
</template>
