<template>
  <UModal v-model:open="open" title="Create New Item" :dismissible="false">
    <template #body>
      <div class="p-4">
        <UForm :schema="schema" :state="state" class="space-y-4" @submit="onSubmit">
          <UFormField label="Item Name / Code" name="itemName" required>
            <UInput
              v-model="state.itemName"
              placeholder="e.g. Stainless Steel Bolt"
              class="w-full"
              :disabled="creating"
            />
          </UFormField>
          <UFormField label="Item Group" name="itemGroup" required>
            <USelectMenu
              v-model="state.itemGroup as any"
              :items="itemGroupOpts"
              value-key="name"
              label-key="name"
              placeholder="Select group..."
              class="w-full"
              :disabled="creating || itemGroupsLoading"
              :loading="itemGroupsLoading"
            />
          </UFormField>
          <UFormField label="Buying Price" name="buyingPrice" required>
            <UInput
              v-model="state.buyingPrice"
              type="number"
              :min="0"
              :step="0.01"
              placeholder="0.00"
              class="w-full"
              :disabled="creating"
            />
          </UFormField>
          <UFormField label="Selling Price" name="sellingPrice" required>
            <UInput
              v-model="state.sellingPrice"
              type="number"
              :min="0"
              :step="0.01"
              placeholder="0.00"
              class="w-full"
              :disabled="creating"
            />
          </UFormField>
          <div class="flex justify-end gap-2 pt-2">
            <UButton color="neutral" variant="outline" :disabled="creating" @click="open = false">
              Cancel
            </UButton>
            <UButton type="submit" color="primary" :loading="creating" :disabled="creating">
              Create Item
            </UButton>
          </div>
        </UForm>
      </div>
    </template>
  </UModal>
</template>

<script setup lang="ts">
import * as z from "zod";
import { reactive, ref, onMounted, watch } from "vue";
import { ErpNextService, type ItemOption } from "@/services/ErpNextService";

const toast = useToast();
const erpnext = new ErpNextService();

const open = defineModel<boolean>("open", { default: false });
const emit = defineEmits<{
  onCreated: [item: ItemOption];
}>();

const creating = ref(false);
const itemGroupsLoading = ref(false);
const itemGroupOpts = ref<{ name: string }[]>([]);

const state = reactive({
  itemName: "",
  itemGroup: "",
  buyingPrice: 0,
  sellingPrice: 0,
});

const schema = z.object({
  itemName: z.string().min(1, "Item name is required"),
  itemGroup: z.string().min(1, "Item group is required"),
  buyingPrice: z.number().gte(0, "Must be >= 0"),
  sellingPrice: z.number().gte(0, "Must be >= 0"),
});

onMounted(async () => {
  itemGroupsLoading.value = true;
  try {
    const groups = await erpnext.getItemGroups();
    if (groups?.length) itemGroupOpts.value = groups;
  } catch {
    /* ignore */
  } finally {
    itemGroupsLoading.value = false;
  }
});

watch(open, (isOpen) => {
  if (isOpen) {
    state.itemName = "";
    state.itemGroup = "";
    state.buyingPrice = 0;
    state.sellingPrice = 0;
  }
});

async function onSubmit() {
  creating.value = true;
  try {
    const result = await erpnext.createItem(
      state.itemName,
      state.itemGroup,
      state.buyingPrice,
      state.sellingPrice,
    );
    if (result) {
      toast.add({ title: "Item created", description: `"${result.item_name}" added successfully`, color: "success" });
      emit("onCreated", result);
      open.value = false;
      state.itemName = "";
      state.itemGroup = "";
      state.buyingPrice = 0;
      state.sellingPrice = 0;
    }
  } catch {
    toast.add({ title: "Error", description: "Failed to create item. It may already exist.", color: "error" });
  } finally {
    creating.value = false;
  }
}
</script>
