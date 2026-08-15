<script setup lang="ts">
import { computed, ref } from "vue";
import { useRoute } from "vue-router";
import * as z from "zod";
import type { AuthFormField, FormSubmitEvent } from "@nuxt/ui";
import { ApiSingleton } from "@/services/api";
import { useAuthStore } from "@/stores/AuthStore";

const route = useRoute();
const authStore = useAuthStore();

const rawToken = route.query.token;
const token = computed(() => (typeof rawToken === "string" ? rawToken : ""));
const isReset = computed(() => Boolean(token.value));

const fields = computed<AuthFormField[]>(() =>
  isReset.value
    ? [
        {
          name: "password",
          type: "password",
          label: "New password",
          placeholder: "At least 8 characters",
          required: true,
          autocomplete: "new-password",
        },
        {
          name: "confirm",
          type: "password",
          label: "Confirm password",
          placeholder: "Repeat your password",
          required: true,
          autocomplete: "new-password",
        },
      ]
    : [
        {
          name: "email",
          type: "email",
          label: "Email",
          placeholder: "Enter your email",
          required: true,
          autocomplete: "email",
        },
      ],
);

const schema = computed(() =>
  isReset.value
    ? z
        .object({
          password: z.string().min(8, "Password must be at least 8 characters."),
          confirm: z.string().min(8, "Password must be at least 8 characters."),
        })
        .refine((data) => data.password === data.confirm, {
          message: "Passwords do not match.",
          path: ["confirm"],
        })
    : z.object({
        email: z.email("Enter a valid email."),
      }),
);

const message = ref("");
const error = ref("");
const loading = ref(false);

type ForgotData = { email: string };
type ResetData = { password: string; confirm: string };

async function onSubmit(event: FormSubmitEvent<ForgotData | ResetData>) {
  error.value = "";
  message.value = "";

  if (!isReset.value) {
    loading.value = true;
    try {
      const api = await ApiSingleton.getInstance();
      await api.POST("/auth/forgot-password", {
        body: { email: (event.data as ForgotData).email },
      });
      message.value = "If an account exists for that email, a reset link has been sent.";
    } catch {
      error.value = "Something went wrong. Please try again.";
    } finally {
      loading.value = false;
    }
    return;
  }

  loading.value = true;
  try {
    const api = await ApiSingleton.getInstance();
    const { data, error: err } = await api.POST("/auth/reset-password", {
      body: { token: token.value, newPassword: (event.data as ResetData).password },
    });
    if (err || !data) {
      error.value = "This reset link is invalid or expired.";
      return;
    }
    authStore.storeSession(data.token, data.user);
    window.location.href = "/";
  } catch {
    error.value = "Something went wrong. Please try again.";
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
        :submit="{
          label: isReset ? 'Save password' : 'Send reset link',
          block: true,
          loading,
        }"
        icon="i-lucide-key-round"
        :title="isReset ? 'Set your password' : 'Reset your password'"
        :description="
          isReset ? 'Choose a new password for your account.' : 'Enter your email and we will send you a reset link.'
        "
        @submit="onSubmit"
      >
        <template #validation>
          <UAlert
            v-if="error"
            color="error"
            variant="subtle"
            icon="i-lucide-circle-alert"
            :title="error"
          />
          <UAlert
            v-else-if="message"
            color="success"
            variant="subtle"
            icon="i-lucide-circle-check"
            :title="message"
          />
        </template>
        <template #footer>
          <RouterLink to="/login" class="text-primary font-medium">
            Back to sign in
          </RouterLink>
        </template>
      </UAuthForm>
    </UCard>
  </div>
</template>
