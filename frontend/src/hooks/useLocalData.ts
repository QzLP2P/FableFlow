import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { bookStore } from "../storage/books";
import { deleteProfile, discardInvalidProfiles, loadProfiles, saveProfile, type ProfileInput } from "../storage/profiles";

const profilesKey = ["profiles"] as const;
export const libraryKey = ["library"] as const;

export function useProfiles() {
  return useQuery({
    queryKey: profilesKey,
    queryFn: async () => loadProfiles(),
    staleTime: Infinity,
    retry: false,
  });
}

export function useSaveProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (input: ProfileInput) => saveProfile(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: profilesKey }),
  });
}

export function useDeleteProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => deleteProfile(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: profilesKey }),
  });
}

export function useDiscardInvalidProfiles() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async () => discardInvalidProfiles(),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: profilesKey }),
  });
}

export function useLibrary() {
  return useQuery({
    queryKey: libraryKey,
    queryFn: () => bookStore.list(),
    staleTime: Infinity,
    retry: false,
  });
}

export function useSavedBook(id: string | undefined) {
  return useQuery({
    queryKey: [...libraryKey, id],
    queryFn: () => bookStore.get(id ?? ""),
    enabled: Boolean(id),
    staleTime: Infinity,
    retry: false,
  });
}

export function useDeleteBooks() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (ids: readonly string[]) => bookStore.remove(ids),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: libraryKey }),
  });
}
