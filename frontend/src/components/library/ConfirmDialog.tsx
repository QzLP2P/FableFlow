import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogContentText from "@mui/material/DialogContentText";
import DialogTitle from "@mui/material/DialogTitle";
import { useId } from "react";

interface ConfirmDialogProps {
  open: boolean;
  title: string;
  description: string;
  confirmLabel: string;
  pending?: boolean;
  onConfirm: () => void;
  onClose: () => void;
}

/** Confirmation d'une suppression définitive ; « Annuler » reçoit le focus par défaut. */
export function ConfirmDialog({ open, title, description, confirmLabel, pending = false, onConfirm, onClose }: ConfirmDialogProps) {
  const titleId = useId();
  const descriptionId = useId();

  return (
    <Dialog open={open} onClose={pending ? undefined : onClose} aria-labelledby={titleId} aria-describedby={descriptionId}>
      <DialogTitle id={titleId}>{title}</DialogTitle>
      <DialogContent>
        <DialogContentText id={descriptionId}>{description}</DialogContentText>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2, gap: 1, flexWrap: "wrap" }}>
        <Button onClick={onClose} disabled={pending} autoFocus size="large">
          Annuler
        </Button>
        <Button onClick={onConfirm} disabled={pending} color="error" variant="contained" size="large">
          {confirmLabel}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
