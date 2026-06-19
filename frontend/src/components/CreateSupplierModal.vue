<template>
  <UModal v-model:open="open" title="Create New Supplier" :dismissible="false">
    <template #body>
      <div class="p-4">
        <UForm :schema="schema" :state="state" class="space-y-4" @submit="onSubmit">
          <UFormField label="Supplier Name" name="supplierName" required>
            <UInput
              v-model="state.supplierName"
              placeholder="e.g. ABC Supplies Ltd"
              class="w-full"
              :disabled="creating"
            />
          </UFormField>
          <div class="flex justify-end gap-2 pt-2">
            <UButton color="neutral" variant="outline" :disabled="creating" @click="open = false">
              Cancel
            </UButton>
            <UButton type="submit" color="primary" :loading="creating" :disabled="creating">
              Create Supplier
            </UButton>
          </div>
        </UForm>
      </div>
    </template>
  </UModal>
</template>

<script setup lang="ts">
import * as z from "zod";
import { reactive, ref, watch } from "vue";
import { ErpNextService, type SupplierOption } from "@/services/ErpNextService";

const toast = useToast();
const erpnext = new ErpNextService();

const open = defineModel<boolean>("open", { default: false });
const emit = defineEmits<{
  onCreated: [item: SupplierOption];
}>();

const creating = ref(false);

const state = reactive({
  supplierName: "",
});

const schema = z.object({
  supplierName: z.string().min(1, "Supplier name is required"),
});

watch(open, (isOpen) => {
  if (isOpen) {
    state.supplierName = "";
  }
});

async function onSubmit() {
  creating.value = true;
  try {
    const result = await erpnext.createSupplier(state.supplierName);
    if (result) {
      toast.add({ title: "Supplier created", description: `"${result.supplier_name}" added successfully`, color: "success" });
      emit("onCreated", result);
      open.value = false;
      state.supplierName = "";
    }
  } catch {
    toast.add({ title: "Error", description: "Failed to create supplier. It may already exist.", color: "error" });
  } finally {
    creating.value = false;
  }
}
</script>
